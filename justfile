#!/usr/bin/env just --justfile

run:
    dotnet run --project src/Ragnar.csproj

eval FILE:
    dotnet run --project src/Ragnar.csproj {{FILE}}

build:
    dotnet build

test:
    dotnet test