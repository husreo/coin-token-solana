#!/usr/bin/env bash
neon_path=~/D_DRIVE/ProgramFiles/neo-compiler/neon/bin/Debug/netcoreapp2.0/publish/neon.dll
output_path=$(pwd)/bin/Debug/netstandard2.0/publish

set -e

echo "Make clean..."
source clean.sh

echo "Building project..."
dotnet publish

echo "Compiling dll to avm..."
dotnet $neon_path $output_path/NEP5Token.dll
