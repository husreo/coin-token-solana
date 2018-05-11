using Neo.Core;

namespace NEP5.Contract.Tests
{
    internal class CustomTransaction : Transaction
    {
        public CustomTransaction(TransactionType type) : base(type)
        {
            Version = 1;
            Inputs = new CoinReference[0];
            Outputs = new TransactionOutput[0];
            Attributes = new TransactionAttribute[0];
        }
    }
}
