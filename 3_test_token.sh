#!/usr/bin/env bash
set -e

echo "Starting test token script..."

echo "Cleaning..."
rm -rf \
    NEP5.Tests/bin \
    NEP5.Tests/obj

echo "Preprocessing..."
./node_modules/.bin/c-preprocessor --config \
    token-config.json \
    NEP5.Tests/Nep5TokenTest.template.cs \
    NEP5.Tests/Nep5TokenTest.cs

echo "Publishing tests..."
dotnet publish NEP5.Tests --configuration Release

echo "Executing tests..."
dotnet test NEP5.Tests --no-build --configuration Release -v n
