using Neo.Lux.Cryptography;
using Neo.Lux.Utils;
using Neo.VM;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Text;

namespace Neo.Emulation.Utils
{
    public static class FormattingUtils
    {
        public static string StackItemAsString(StackItem item, bool addQuotes = false, Emulator.Type hintType = Emulator.Type.Unknown)
        {
            if (item is ICollection)
            {
                var bytes = item.GetByteArray();
                if (bytes != null && bytes.Length == 20)
                {
                    var signatureHash = new UInt160(bytes);
                    return CryptoUtils.ToAddress(signatureHash);
                }

                var s = new StringBuilder();
                var items = (ICollection)item;

                s.Append('[');
                int i = 0;
                foreach (StackItem element in items)
                {
                    if (i > 0)
                    {
                        s.Append(',');
                    }
                    s.Append(StackItemAsString(element));

                    i++;
                }
                s.Append(']');


                return s.ToString();
            }

            if (item is Neo.VM.Types.Boolean && hintType == Emulator.Type.Unknown)
            {
                return item.GetBoolean().ToString();
            }

            if (item is Neo.VM.Types.Integer && hintType == Emulator.Type.Unknown)
            {
                return item.GetBigInteger().ToString();
            }

            if (item is Neo.VM.Types.InteropInterface)
            {
                return "{InteropInterface}";
            }

            byte[] data = null;
            
            try {
                data = item.GetByteArray();
            }
            catch
            {

            }
            
            if ((data == null || data.Length == 0) && hintType == Emulator.Type.Unknown)
            {
                return "Null";
            }

            if (hintType == Emulator.Type.Array)
            {
                var s = new StringBuilder();
                s.Append('[');
                int count = 0;
                if (data.Length > 0)
                {
                    var array = (VM.Types.Array)item;
                    foreach (var entry in array)
                    {
                        if (count > 0)
                        {
                            s.Append(", ");
                        }
                        count++;

                        s.Append(StackItemAsString(entry, addQuotes, Emulator.Type.Unknown));
                    }
                }
                s.Append(']');

                return s.ToString();
            }

            return FormattingUtils.OutputData(data, addQuotes, hintType);
        }

        private enum ContractParameterTypeLocal : byte
        {
            Signature = 0,
            Boolean = 1,
            Integer = 2,
            Hash160 = 3,
            Hash256 = 4,
            ByteArray = 5,
            PublicKey = 6,
            String = 7,
            Array = 16,
            InteropInterface = 240,
            Void = 255
        };

        public static string OutputData(byte[] data, bool addQuotes, Emulator.Type hintType = Emulator.Type.Unknown)
        {
            if (data == null)
            {
                return "Null";
            }

            byte[] separator = { Convert.ToByte(';') };
            int dataLen = data.Length;
            if (dataLen > 5 && /* {a:...;} */
                (char)data[0] == '{' && (char)data[dataLen - 1] == '}') // Binary NeoStorageKey
            {
                byte[][] parts = ByteArraySplit(data.SubArray(1, dataLen - 1), separator);
                Debug.WriteLine("parts.len " + parts.Length.ToString());
                string keyString = "{";
                foreach (byte[] part in parts)
                {
                    Debug.WriteLine("part.len " + part.Length.ToString());
                    if (part.Length >= 4)
                    {
                        byte fieldCode = part[0];
                        byte fieldType = part[2];
                        byte[] fieldValueAsBytes = part.SubArray(4, part.Length - 4);
                        string fieldValueAsString = System.Text.Encoding.ASCII.GetString(fieldValueAsBytes);
                        Debug.WriteLine("fieldCode " + ((char)fieldCode).ToString() + " fieldType " + ((int)fieldType).ToString());
                        Debug.WriteLine("fieldValue " + fieldValueAsBytes.ByteToHex() + " '" + fieldValueAsString + "'");
                        switch ((char)fieldCode)
                        {
                            case '#': // signature and flags
                                {
                                    keyString += "#:" + ((int)fieldType).ToString() + "=" + fieldValueAsBytes.ByteToHex();
                                    break;
                                }
                            case 'a': // app name
                                {
                                    keyString += "a:" + ((int)fieldType).ToString() + "=" + fieldValueAsString;
                                    break;
                                }
                            case 'M': // app major version
                                {
                                    BigInteger fieldValueAsBigInteger = new BigInteger(fieldValueAsBytes);
                                    keyString += "M:" + ((int)fieldType).ToString() + "=" + fieldValueAsBigInteger.ToString();
                                    break;
                                }
                            case 'm': // app minor version
                                {
                                    BigInteger fieldValueAsBigInteger = new BigInteger(fieldValueAsBytes);
                                    keyString += "m:" + ((int)fieldType).ToString() + "=" + fieldValueAsBigInteger.ToString();
                                    break;
                                }
                            case 'b': // app build number
                                {
                                    BigInteger fieldValueAsBigInteger = new BigInteger(fieldValueAsBytes);
                                    keyString += "b:" + ((int)fieldType).ToString() + "=" + fieldValueAsBigInteger.ToString();
                                    break;
                                }
                            case 'u': // userScriptHash (integer or binary)
                                {
                                    switch (fieldType)
                                    {
                                        case (int)ContractParameterTypeLocal.Integer:
                                            {
                                                BigInteger fieldValueAsBigInteger = new BigInteger(fieldValueAsBytes);
                                                keyString += "u:" + ((int)fieldType).ToString() + "=" + fieldValueAsBigInteger.ToString();
                                                break;
                                            }
                                        case (int)ContractParameterTypeLocal.String:
                                            {
                                                keyString += "u:" + ((int)fieldType).ToString() + "=" + fieldValueAsString;
                                                break;
                                            }
                                        case (int)ContractParameterTypeLocal.ByteArray:
                                            {
                                                keyString += "u:" + ((int)fieldType).ToString() + "=" + fieldValueAsBytes.ByteToHex();
                                                break;
                                            }
                                        default:
                                            {
                                                keyString += "u?:" + ((int)fieldType).ToString() + "=" + fieldValueAsBytes.ByteToHex();
                                                break;
                                            }
                                    }
                                    break;
                                }
                            case 'c': // class name
                                {
                                    keyString += "c:" + ((int)fieldType).ToString() + "=" + fieldValueAsString;
                                    break;
                                }
                            case 'i': // index
                                {
                                    BigInteger fieldValueAsBigInteger = new BigInteger(fieldValueAsBytes);
                                    keyString += "i:" + ((int)fieldType).ToString() + "=" + fieldValueAsBigInteger.ToString();
                                    break;
                                }
                            case 'f': // field name
                                {
                                    keyString += "c:" + ((int)fieldType).ToString() + "=" + fieldValueAsString;
                                    break;
                                }
                            default:
                                {
                                    keyString += ((char)fieldCode).ToString() + "?:" + ((int)fieldType).ToString() + "=" + fieldValueAsBytes.ByteToHex();
                                    break;
                                }
                        }
                        keyString += ";";
                    }
                }
                keyString += "}";

                return keyString;
            }
            else
            {
                if (hintType != Emulator.Type.Unknown)
                {
                    switch (hintType)
                    {
                        case Emulator.Type.String:
                            {
                                var val = System.Text.Encoding.UTF8.GetString(data);
                                if (addQuotes)
                                {
                                    val= '"' + val+ '"';
                                }
                                return val;
                            }

                        case Emulator.Type.Boolean:
                            {
                                return (data != null && data.Length > 0 && data[0] != 0) ? "True" : "False";
                            }

                        case Emulator.Type.Integer:
                            {
                                return new BigInteger(data).ToString();
                            }
                    }
                }

                for (int i = 0; i < dataLen; i++)
                {
                    var c = (char)data[i];


                    var isValidText = char.IsLetterOrDigit(c) || char.IsPunctuation(c) || char.IsWhiteSpace(c) 
                                                              || "!@#$%^&*()-=_+[]{}|;':,./<>?".Contains(c.ToString());
                    if (!isValidText)
                    {
                        if (data.Length == 20)
                        {
                            var signatureHash = new UInt160(data);
                            return CryptoUtils.ToAddress(signatureHash);
                        }

                        return data.ByteToHex();
                    }
                }

            }

            var result = System.Text.Encoding.ASCII.GetString(data);

            if (addQuotes)
            {
                result = '"' + result + '"';
            }

            return result;
        }

        // Separate() from: https://stackoverflow.com/questions/9755090/split-a-byte-array-at-a-delimiter
        public static byte[][] ByteArraySplit(byte[] source, byte[] separator)
        {
            var parts = new List<byte[]>();
            var index = 0;
            byte[] part;
            for (var i = 0; i < source.Length; ++i)
            {
                if (Equals(source, separator, i))
                {
                    part = new byte[i - index];
                    Array.Copy(source, index, part, 0, part.Length);
                    parts.Add(part);
                    index = i + separator.Length;
                    i += separator.Length - 1;
                }
            }
            part = new byte[source.Length - index];
            Array.Copy(source, index, part, 0, part.Length);
            parts.Add(part);
            return parts.ToArray();
        }

        // https://stackoverflow.com/questions/9755090/split-a-byte-array-at-a-delimiter
        private static bool Equals(byte[] source, byte[] separator, int index)
        {
            for (int i = 0; i < separator.Length; ++i)
            {
                if (index + i >= source.Length || source[index + i] != separator[i]) return false;
            }
            return true;
        }
    }
}
