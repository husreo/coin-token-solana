#!/usr/bin/env bash
set -e

echo "Cleaning..."
source scripts/clean.sh

echo "Preprocessing..."
source scripts/preprocess.sh

echo "Building project..."
dotnet publish

echo "Compiling dll to avm..."
dotnet \
    $(pwd)/neo-compiler/neon/bin/Debug/netcoreapp2.0/publish/neon.dll \
    $(pwd)/bin/Debug/netstandard2.0/publish/NEP5Token.dll
