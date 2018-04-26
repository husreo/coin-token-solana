#!/usr/bin/env bash
neon_path=~/D_DRIVE/ProgramFiles/neo-compiler/neon/bin/Debug/netcoreapp2.0/publish/neon.dll
output_path=$(pwd)/bin/Debug/netstandard2.0

set -e

echo "Make clean..."
source clean.sh

echo "Building project..."
dotnet build

echo "Copying Net.Framework to output directory..."
cp ~/.nuget/packages/neo.smartcontract.framework/2.7.3/lib/netstandard1.6/Neo.SmartContract.Framework.dll \
$output_path/Neo.SmartContract.Framework.dll

echo "Compiling dll to avm..."
dotnet $neon_path $output_path/NEP5Token.dll
