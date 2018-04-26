#!/usr/bin/env bash
set -e

echo "Cleaning..."
rm -rf \
    NEP5Token/bin \
    NEP5Token/obj \
    NEP5Token/Nep5Token.cs

echo "Preprocessing..."
source scripts/preprocess.sh

echo "Building project..."
dotnet publish --configuration Release NEP5Token

echo "Compiling dll to avm..."
dotnet \
    $(pwd)/neo-compiler/neon/bin/Release/netcoreapp2.0/publish/neon.dll \
    $(pwd)/NEP5Token/bin/Release/netcoreapp2.0/publish/NEP5Token.dll
