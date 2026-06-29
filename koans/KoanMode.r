substitute-blank: func [expr val] [
    either paren? :expr [
        res: copy []
        foreach item expr [
            append res substitute-blank :item :val
        ]
        to-paren res
    ] [
        either record? :expr [
            res: copy []
            foreach item expr [
                append res substitute-blank :item :val
            ]
            to-record res
        ] [
            either block? :expr [
                res: copy []
                foreach item expr [
                    append res substitute-blank :item :val
                ]
                res
            ] [
                either (to-string :expr) = "__" [
                    :val
                ] [
                    :expr
                ]
            ]
        ]
    ]
]

run-koan-file: func [file name /local meditations med desc expr hint user-input parsed substituted result correct?] [
    meditations: load file
    if not block? meditations [
        print rejoin ["Error: meditations file " to-string file " does not contain a block."]
        return none
    ]
    
    print rejoin ["Starting Koan: " name]
    
    idx: 1
    while [idx <= length? meditations] [
        med: pick meditations idx
        desc: select med 'description
        expr: select med 'expression
        hint: select med 'hint
        
        print ""
        print rejoin ["Meditation " idx ": " desc]
        print rejoin ["  Expression: " mold expr]
        
        correct?: false
        while [not correct?] [
            user-input: ask "  Answer: "
            user-input: trim user-input
            
            either any [ (lowercase user-input) = "q" (lowercase user-input) = "quit" ] [
                print "Returning to menu..."
                return none
            ] [
                parsed: attempt [load user-input]
                either none? :parsed [
                    print "  Error: Could not parse your answer. Try again."
                ] [
                    substituted: substitute-blank expr :parsed
                    result: attempt [do substituted]
                    
                    either all [ not none? :result logic? :result result ] [
                        print "  Correct! Well done."
                        correct?: true
                    ] [
                        print rejoin ["  Wrong answer. Hint: " hint]
                    ]
                ]
            ]
        ]
        idx: idx + 1
    ]
    print ""
    print "Congratulations! You have completed all meditations in this koan!"
    ask "Press Enter to return to the menu..."
]

start-koan-mode: func [/local koans-list idx choice num item file name] [
    koans-list: [
        [file: %koans/01-equalities.r name: "Equalities"]
        [file: %koans/02-blocks-and-series.r name: "Blocks and Series"]
        [file: %koans/03-functions.r name: "Functions"]
        [file: %koans/04-conditionals.r name: "Conditionals"]
        [file: %koans/05-loops-and-recursion.r name: "Loops and Recursion"]
        [file: %koans/06-math-and-operators.r name: "Math and Operators"]
        [file: %koans/07-strings-and-characters.r name: "Strings and Characters"]
        [file: %koans/08-objects-and-records.r name: "Objects and Records"]
        [file: %koans/09-binding-and-scope.r name: "Binding and Scope"]
        [file: %koans/10-dotnet-interop.r name: ".NET Interoperability"]
    ]
    
    while [true] [
        print ""
        print "=== Ragnar Koans Menu ==="
        idx: 1
        foreach item koans-list [
            name: select item 'name
            print rejoin ["  " idx ". " name]
            idx: idx + 1
        ]
        print "  q. Quit Koan Mode"
        print ""
        
        choice: ask "Select a koan (1-10) or 'q' to quit: "
        choice: lowercase trim choice
        
        either choice = "q" [
            print "Exiting Koan Mode. Happy coding!"
            break
        ] [
            num: attempt [to-integer choice]
            either all [integer? num num >= 1 num <= length? koans-list] [
                item: pick koans-list num
                file: join system/options/boot select item 'file
                name: select item 'name
                
                either exists? file [
                    run-koan-file file name
                ] [
                    print rejoin ["Error: File " to-string file " does not exist."]
                ]
            ] [
                print "Invalid selection. Please enter a number between 1 and 10, or 'q'."
            ]
        ]
    ]
]
