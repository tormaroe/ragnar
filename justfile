#!/usr/bin/env just --justfile

run:
    dotnet run --project src/Ragnar.csproj

build:
    dotnet build

test:
    dotnet test