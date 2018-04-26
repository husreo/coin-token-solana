#!/usr/bin/env bash
set -e

echo "Installing npm packages..."
npm install

echo "Publishing neo-compiler..."
dotnet publish neo-compiler/neon/neon.csproj