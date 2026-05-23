#!/usr/bin/env just --justfile

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

site:
    node docs/server.js