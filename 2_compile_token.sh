#!/usr/bin/env bash
set -e

echo "Starting compile token script..."

echo "Cleaning..."
rm -rf \
    Common/bin \
    Common/obj \
    NEP5.Contract/bin \
    NEP5.Contract/obj \
    NEP5.Contract/Nep5Token.cs

echo "Preprocessing..."
./node_modules/.bin/c-preprocessor --config \
    token-config.json \
    NEP5.Contract/Nep5Token.template.cs \
    NEP5.Contract/Nep5Token.cs

echo "Publishing contract..."
dotnet publish --configuration Release NEP5.Contract

echo "Compiling dll to avm..."
dotnet \
    $(pwd)/neo-compiler/neon/bin/Release/netcoreapp2.0/publish/neon.dll \
    $(pwd)/NEP5.Contract/bin/Release/netcoreapp2.0/publish/NEP5.Contract.dll
