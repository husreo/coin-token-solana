#!/usr/bin/env bash
set -e

echo "Starting compile crowdsale script..."

echo "Cleaning..."
rm -rf \
    Crowdsale.Contract/bin \
    Crowdsale.Contract/obj \
    Crowdsale.Contract/Crowdsale.cs

echo "Preprocessing..."
./node_modules/.bin/c-preprocessor --config \
    crowdsale-config.json \
    Crowdsale.Contract/Crowdsale.template.cs \
    Crowdsale.Contract/Crowdsale.cs

echo "Publishing contract..."
dotnet publish --configuration Release Crowdsale.Contract

echo "Compiling dll to avm..."
dotnet \
    $(pwd)/neo-compiler/neon/bin/Release/netcoreapp2.0/publish/neon.dll \
    $(pwd)/Crowdsale.Contract/bin/Release/netcoreapp2.0/publish/Crowdsale.Contract.dll
