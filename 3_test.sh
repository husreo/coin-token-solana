#!/usr/bin/env bash
set -e

echo "Cleaning..."
rm -rf \
    Tests/bin \
    Tests/obj

echo "Publishing tests..."
dotnet publish Tests --configuration Release

echo "Executing tests..."
dotnet test Tests --no-build --configuration Release -v n