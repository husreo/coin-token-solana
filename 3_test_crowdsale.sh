#!/usr/bin/env bash
set -e

echo "Starting test crowdsale script..."

echo "Cleaning..."
rm -rf \
    Crowdsale.Tests/bin \
    Crowdsale.Tests/obj

echo "Preprocessing..."
./node_modules/.bin/c-preprocessor --config \
    crowdsale-config.json \
    Crowdsale.Tests/CrowdsaleTest.template.cs \
    Crowdsale.Tests/CrowdsaleTest.cs

echo "Publishing tests..."
dotnet publish Crowdsale.Tests --configuration Release

echo "Executing tests..."
dotnet test Crowdsale.Tests --no-build --configuration Release -v n
