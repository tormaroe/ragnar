#!/usr/bin/env just --justfile

set windows-shell := ["powershell.exe", "-NoProfile", "-Command"]

run:
    dotnet run --project src/Ragnar.csproj

eval FILE:
    dotnet run --project src/Ragnar.csproj {{FILE}}

build:
    dotnet build

test:
    dotnet test

deploy:
    dotnet publish src/Ragnar.csproj -c Release -o dist -p:PublishSingleFile=true -p:PublishReadyToRun=true -p:SelfContained=false

release:
    powershell -NoProfile -ExecutionPolicy Bypass -File scripts/release.ps1

site:
    node docs/server.js