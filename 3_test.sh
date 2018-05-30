#!/usr/bin/env bash
set -e

echo "Cleaning..."
rm -rf \
    NEP5.Contract.Tests/bin \
    NEP5.Contract.Tests/obj

echo "Preprocessing..."
./node_modules/.bin/c-preprocessor --config \
    c-preprocessor-config.json \
    NEP5.Contract.Tests/Nep5TokenTest.template.cs \
    NEP5.Contract.Tests/Nep5TokenTest.cs

echo "Publishing tests..."
dotnet publish NEP5.Contract.Tests --configuration Release

echo "Executing tests..."
dotnet test NEP5.Contract.Tests --no-build --configuration Release -v n