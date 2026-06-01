#!/usr/bin/env ragnar

args: system/options/args

; 1. Determine directory to check
target-dir: either empty? args ["."] [first args]

print rejoin ["Checking repository status in: " target-dir]

; 2. Ensure directory exists
either exists? to-file target-dir [
    cd to-file target-dir
] [
    print rejoin ["Error: Directory '" target-dir "' does not exist!"]
    quit/with 1
]

; 3. Check for .git directory
either exists? %.git [
    print "Git repository detected."
    
    ; Get branch name
    branch: trim call/output "git branch --show-current"
    print rejoin ["Current branch: " branch]
    
    ; Check status summary
    status: trim call/output "git status --short"
    either empty? status [
        print "Working tree is clean."
    ] [
        print "Uncommitted changes found:"
        print status
    ]
] [
    print "Not a git repository (no .git folder found)."
    quit/with 1
]

quit/with 0
