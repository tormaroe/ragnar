#!/usr/bin/env ragnar

args: system/options/args

show-help: does [
    print "Usage: ./do <command> [args]"
    print ""
    print "Commands:"
    print "  run         - Run ragnar REPL (compiled on-the-fly)"
    print "  eval <file> - Evaluate a specific ragnar file"
    print "  build       - Build the Ragnar project"
    print "  test        - Run the test suite"
    print "  deploy      - Publish a single-file executable to dist/"
    print "  release     - Run the release script"
    print "  bump        - Trigger version bump manually"
    print "  site        - Run the local documentation website server"
]

either empty? args [
    show-help
] [
    cmd: first args
    cmd-args: next args

    switch/default cmd [
        "run" [
            call/wait/shell "dotnet run --project src/Ragnar.csproj"
        ]
        "eval" [
            either empty? cmd-args [
                print "Error: eval requires a file path."
                quit/with 1
            ] [
                file: first cmd-args
                call/wait/shell rejoin ["dotnet run --project src/Ragnar.csproj " file]
            ]
        ]
        "build" [
            call/wait/shell "dotnet build"
        ]
        "test" [
            call/wait/shell "dotnet test"
        ]
        "deploy" [
            call/wait/shell "dotnet publish src/Ragnar.csproj -c Release -o dist -p:PublishSingleFile=true -p:PublishReadyToRun=true -p:SelfContained=false"
        ]
        "release" [
            exit-code: call/wait/shell "python3 scripts/release.py"
            if not-equal? exit-code 0 [
                call/wait/shell "python scripts/release.py"
            ]
        ]
        "bump" [
            call/wait/shell ".githooks/pre-commit --force"
        ]
        "site" [
            call/wait/shell "node docs/server.js"
        ]
    ] [
        print ["Unknown command: " cmd]
        print ""
        show-help
        quit/with 1
    ]
]
