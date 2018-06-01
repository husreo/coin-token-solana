#!/usr/bin/env bash
set -e

echo "Pulling submodule"
git submodule update --init --recursive

echo "Installing npm packages..."
npm install

echo "Publishing neo-compiler..."
dotnet publish neo-compiler/neon/neon.csproj --configuration Release