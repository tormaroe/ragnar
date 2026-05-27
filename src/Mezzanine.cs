namespace Ragnar;

public static class Mezzanine
{
    public const string SOURCE = """
        block?: func ["Returns true if the value is a block." v] [ (type? :v) = 'block! ]
        paren?: func ["Returns true if the value is a paren." v] [ (type? :v) = 'paren! ]
        record?: func ["Returns true if the value is a record." v] [ (type? :v) = 'record! ]
        text?: func ["Returns true if the value is a string (Text)." v] [ (type? :v) = 'text! ]
        string?: func ["Returns true if the value is a string." v] [ (type? :v) = 'text! ]
        integer?: func ["Returns true if the value is an integer." v] [ (type? :v) = 'integer! ]
        decimal?: func ["Returns true if the value is a decimal." v] [ (type? :v) = 'decimal! ]
        logic?: func ["Returns true if the value is a logic (boolean)." v] [ (type? :v) = 'logic! ]
        word?: func ["Returns true if the value is a word type." v] [
            any [
                (type? :v) = 'word!
                (type? :v) = 'lit-word!
                (type? :v) = 'set-word!
                (type? :v) = 'get-word!
            ]
        ]

        |: func ["Pipe previous result into a function of arity 1." fn] [ fn it ]
        |>: func ["Pipe previous result into a function of arity 2 as the first argument." fn x] [ fn it x ]
        >|: func ["Pipe previous result into a function of arity 2 as the second argument." fn x] [ fn x it ]
        |>>: func ["Pipe previous result into a function of arity 3 as the first argument." fn x y] [ fn it x y ]
        >|>: func ["Pipe previous result into a function of arity 3 as the second argument." fn x y] [ fn x it y ]
        >>|: func ["Pipe previous result into a function of arity 3 as the third argument." fn x y] [ fn x y it ]

        ask: func ["Prompts the user for input and returns it as a string." prompt] [
            prin prompt
            input
        ]
        confirm: func ["Prompts the user to confirm an action." question /with options] [
            choices: either with [options] [ ["y" "n"] ]
            prompt: rejoin [question " (" join "/" choices ") "]
            
            while [true] [
                prin prompt
                response: lowercase trim input
                
                ; Find if the response is in our choices
                found: false
                idx: 1
                while [idx <= length? choices] [
                    if response = lowercase pick choices idx [
                        found: true
                        break
                    ]
                    idx: idx + 1
                ]
                
                if found [
                    either with [
                        return pick choices idx
                    ] [
                        return idx = 1 ; Return logic! true if "y" (idx 1), false if "n" (idx 2)
                    ]
                ]
                
                print rejoin ["Please enter one of: " join ", " choices "."]
            ]
        ]
        does: func ["Defines a function with no arguments." block] [ func [] block ]
        funcmap: func ["Applies a function to each item in a block and returns the results." fn block] [
            result: copy []
            foreach item block [
                append result fn :item
            ]
            result
        ]
        funcflatmap: func ["Applies a function to each item in a block and flattens the results." fn block] [
            result: copy []
            foreach item block [
                val: fn :item
                either block? :val [
                    foreach sub-item val [
                        append result :sub-item
                    ]
                ] [
                    append result :val
                ]
            ]
            result
        ]
        funcfilter: func ["Filters a block using a function that returns true for items to keep." fn block] [
            result: copy []
            foreach item block [
                if fn :item [ append result :item ]
            ]
            result
        ]
        funcfold: func ["Reduces a block to a single value using a binary function." fn block /initial initial-value] [
            if empty? block [ return either initial [initial-value] [none] ]
            acc: either initial [initial-value] [first block]
            idx: either initial [1] [2]
            while [idx <= length? block] [
                acc: fn :acc pick block idx
                idx: idx + 1
            ]
            acc
        ]
        get-env: func ["Returns the value of an environment variable." name] [call-static "System.Environment" "GetEnvironmentVariable" reduce [name]]
        enumerate: func ["Iterates a .NET IEnumerable, binding word to each item and evaluating block." enumerable 'word block] [
            enumerator: call-method enumerable "GetEnumerator" []
            while [call-method enumerator "MoveNext" []] [
                set word enumerator/current
                do block
            ]
        ]
        ignore: func ["Consume one argument and return none." x] [ none ]
        _: func ["Consume one argument and return none (alias for ignore)." x] [ none ]
        list-env: func ["Lists all environment variables as a block of name-value pairs."] [
            vars: call-static "System.Environment" "GetEnvironmentVariables" []
            result: copy []
            enumerate vars item [
                append result item/key 
                append result item/value
            ]
            result
        ]
        map-each: func ["Evaluates a block for each value(s) in a series and returns them as a block." 'word data body] [
            words: either block? :word [:word] [append copy [] :word]
            if empty? words [return []]
            
            orig-values: copy []
            foreach w words [
                append orig-values attempt [get w]
            ]
            
            results: copy []
            cursor: data
            
            while [not empty? cursor] [
                foreach w words [
                    val: either empty? cursor [none] [first cursor]
                    set w :val
                    if not empty? cursor [cursor: next cursor]
                ]
                append results do body
            ]
            
            idx: 1
            while [idx <= length? words] [
                w: pick words idx
                val: pick orig-values idx
                set w :val
                idx: idx + 1
            ]
            
            results
        ]
        max: func ["Returns the greater of two values." a b] [ either greater? a b [a] [b] ]
        min: func ["Returns the lesser of two values." a b] [ either greater? a b [b] [a] ]
        negate: func ["Returns the negative of a number." n] [ n * -1 ]
        none?: func ["Returns true if the value is none." x] [ equal? x none ]
        not-equal?: func ["Returns true if two values are not equal." a b] [ not equal? a b ]
        now: func ["Returns the current System.DateTime or specific parts." /time /date /year /month /day] [
            dt: get-static "System.DateTime" "Now"
            if time [ return call-method dt "ToLongTimeString" [] ]
            if date [ return call-method dt "ToShortDateString" [] ]
            if year [ return get-prop dt "Year" ]
            if month [ return get-prop dt "Month" ]
            if day [ return get-prop dt "Day" ]
            dt
        ]
        pwd: func ["Returns the current working directory."] [what-dir]
        rc-file-name: %.ragnar.r
        reform: func ["Evaluates a block and forms a string with spaces between values." block] [
            result: ""
            is-first: true
            foreach val reduce block [
                either is-first [
                    result: to-string val
                    is-first: false
                ] [
                    result: rejoin [result " " val]
                ]
            ]
            result
        ]
        rejoin: func ["Reduces and joins a block of values into a string." block] [
            block: reduce block
            either empty? block [block] [
                join first block next block
            ]
        ]
        what-dir: func ["Returns the current working directory."] [to-file call-static "System.IO.Directory" "GetCurrentDirectory" []]
        wait: func ["Wait for a number of milliseconds." ms] [call-static "System.Threading.Thread" "Sleep" reduce [ms]]
        zero?: func ["Returns true if the value is zero." x] [ x = 0 ]
        switch: func ["Selects a choice and evaluates the first block that follows it." value cases /default case-default] [
            result: select cases value
            either block? :result [
                do result
            ] [
                either default [ do case-default ] [ none ]
            ]
        ]

        cd: func ["Changes the current working directory." target] [
            call-static "System.IO.Directory" "SetCurrentDirectory" reduce [to-string target]
            what-dir
        ]

        ls: func ["Returns a block of paths in the current directory as file values." /all] [
            show-all?: :all
            entries: call-static "System.IO.Directory" "GetFileSystemEntries" reduce [what-dir]
            result: copy []
            enumerate entries entry [
                name: call-static "System.IO.Path" "GetFileName" reduce [entry]
                show?: true
                if not :show-all? [
                    either (pick name 1) = "." [
                        show?: false
                    ] [
                        attr: call-static "System.IO.File" "GetAttributes" reduce [entry]
                        hidden-enum: get-static "System.IO.FileAttributes" "Hidden"
                        if call-method attr "HasFlag" reduce [hidden-enum] [ show?: false ]
                    ]
                ]
                if show? [
                    is-dir?: call-static "System.IO.Directory" "Exists" reduce [entry]
                    either is-dir? [
                        append result to-file join name "/"
                    ] [
                        append result to-file name
                    ]
                ]
            ]
            result
        ]

        mkdir: func ["Creates a directory and any parent directories if they don't exist." path /verbose /v] [
            str-path: to-string path
            exists?: call-static "System.IO.Directory" "Exists" reduce [str-path]
            either exists? [
                if any [:verbose :v] [
                    print rejoin ["Directory already exists: " str-path]
                ]
            ] [
                if any [:verbose :v] [
                    print rejoin ["Creating directory: " str-path]
                ]
                call-static "System.IO.Directory" "CreateDirectory" reduce [str-path]
            ]
            none
        ]

        rmdir: func ["Removes a directory if it is empty." path /verbose /v] [
            str-path: to-string path
            exists?: call-static "System.IO.Directory" "Exists" reduce [str-path]
            either not exists? [
                throw rejoin ["Directory does not exist: " str-path]
            ] [
                entries: call-static "System.IO.Directory" "GetFileSystemEntries" reduce [str-path]
                either entries/Length = 0 [
                    if any [:verbose :v] [
                        print rejoin ["Removing directory: " str-path]
                    ]
                    call-static "System.IO.Directory" "Delete" reduce [str-path]
                ] [
                    throw rejoin ["Directory is not empty: " str-path]
                ]
            ]
            none
        ]

        rm-helper: func [entry recursive verbose interactive] [
            is-dir?: call-static "System.IO.Directory" "Exists" reduce [entry]
            either is-dir? [
                either recursive [
                    children: call-static "System.IO.Directory" "GetFileSystemEntries" reduce [entry]
                    enumerate children child [
                        rm-helper child recursive verbose interactive
                    ]
                    should-delete?: true
                    if interactive [
                        should-delete?: confirm rejoin ["remove directory " entry "?"]
                    ]
                    if should-delete? [
                        if verbose [ print rejoin ["Removing directory: " entry] ]
                        call-static "System.IO.Directory" "Delete" reduce [entry]
                    ]
                ] [
                    print rejoin ["rm: " entry ": is a directory"]
                ]
            ] [
                should-delete?: true
                if interactive [
                    should-delete?: confirm rejoin ["remove file " entry "?"]
                ]
                if should-delete? [
                    if verbose [ print rejoin ["Removing file: " entry] ]
                    call-static "System.IO.File" "Delete" reduce [entry]
                ]
            ]
        ]

        rm: func ["Removes files or directories." path /recursive /r /verbose /v /interactive /i] [
            str-path: to-string path
            is-recursive: any [:recursive :r]
            is-verbose: any [:verbose :v]
            is-interactive: any [:interactive :i]
            
            has-wildcard?: any [
                not none? find str-path "*"
                not none? find str-path "?"
            ]
            
            either has-wildcard? [
                dir: call-static "System.IO.Path" "GetDirectoryName" reduce [str-path]
                pattern: call-static "System.IO.Path" "GetFileName" reduce [str-path]
                if any [none? dir empty? dir] [ dir: what-dir ]
                if any [none? pattern empty? pattern] [ pattern: "*" ]
                
                dir-exists?: call-static "System.IO.Directory" "Exists" reduce [dir]
                either dir-exists? [
                    entries: call-static "System.IO.Directory" "GetFileSystemEntries" reduce [dir pattern]
                    enumerate entries entry [
                        rm-helper entry is-recursive is-verbose is-interactive
                    ]
                ] [
                    print rejoin ["rm: " dir ": No such directory"]
                ]
            ] [
                exists-file?: call-static "System.IO.File" "Exists" reduce [str-path]
                exists-dir?: call-static "System.IO.Directory" "Exists" reduce [str-path]
                either any [exists-file? exists-dir?] [
                    rm-helper str-path is-recursive is-verbose is-interactive
                ] [
                    print rejoin ["rm: " str-path ": No such file or directory"]
                ]
            ]
            none
        ]

        pushd: func ["Pushes the current directory onto the stack and changes to the target directory." target] [
            if not block? attempt [system/dir-stack] [
                system/dir-stack: copy []
            ]
            append system/dir-stack what-dir
            cd target
        ]

        popd: func ["Pops a directory from the stack and changes to it."] [
            stack: attempt [system/dir-stack]
            either any [none? stack empty? :stack] [
                print "Directory stack is empty"
                none
            ] [
                target: take back tail stack
                cd target
            ]
        ]

        mv: func ["Moves/renames a file or directory." src dest /force /f /verbose /v] [
            src-str: to-string src
            dest-str: to-string dest
            
            is-dir?: call-static "System.IO.Directory" "Exists" reduce [src-str]
            is-file?: call-static "System.IO.File" "Exists" reduce [src-str]
            
            either not any [is-dir? is-file?] [
                print rejoin ["mv: " src-str ": No such file or directory"]
            ] [
                allow-overwrite: any [:force :f]
                is-v: any [:verbose :v]
                
                either is-dir? [
                    dest-exists-dir?: call-static "System.IO.Directory" "Exists" reduce [dest-str]
                    dest-exists-file?: call-static "System.IO.File" "Exists" reduce [dest-str]
                    
                    if any [dest-exists-dir? dest-exists-file?] [
                        either allow-overwrite [
                            if dest-exists-dir? [
                                call-static "System.IO.Directory" "Delete" reduce [dest-str true]
                            ]
                            if dest-exists-file? [
                                call-static "System.IO.File" "Delete" reduce [dest-str]
                            ]
                        ] [
                            throw rejoin ["Destination path already exists: " dest-str]
                        ]
                    ]
                    
                    if is-v [
                        print rejoin ["Moving directory: " src-str " to " dest-str]
                    ]
                    call-static "System.IO.Directory" "Move" reduce [src-str dest-str]
                ] [
                    dest-exists-dir?: call-static "System.IO.Directory" "Exists" reduce [dest-str]
                    dest-exists-file?: call-static "System.IO.File" "Exists" reduce [dest-str]
                    
                    if dest-exists-dir? [
                        filename: call-static "System.IO.Path" "GetFileName" reduce [src-str]
                        dest-str: call-static "System.IO.Path" "Combine" reduce [dest-str filename]
                        dest-exists-file?: call-static "System.IO.File" "Exists" reduce [dest-str]
                    ]
                    
                    if dest-exists-file? [
                        either allow-overwrite [
                            call-static "System.IO.File" "Delete" reduce [dest-str]
                        ] [
                            throw rejoin ["Destination file already exists: " dest-str]
                        ]
                    ]
                    
                    if is-v [
                        print rejoin ["Moving file: " src-str " to " dest-str]
                    ]
                    call-static "System.IO.File" "Move" reduce [src-str dest-str]
                ]
            ]
            none
        ]

        cp-file-helper: func [src dest overwrite verbose] [
            exists?: call-static "System.IO.File" "Exists" reduce [dest]
            either exists? [
                either overwrite [
                    if verbose [ print rejoin ["Copying file: " src " to " dest " (overwrite)"] ]
                    call-static "System.IO.File" "Copy" reduce [src dest true]
                ] [
                    throw rejoin ["Destination file already exists: " dest]
                ]
            ] [
                if verbose [ print rejoin ["Copying file: " src " to " dest] ]
                call-static "System.IO.File" "Copy" reduce [src dest false]
            ]
        ]

        cp: func ["Copies files." src dest /force /f /verbose /v] [
            src-str: to-string src
            dest-str: to-string dest
            
            allow-overwrite: any [:force :f]
            is-v: any [:verbose :v]
            
            has-wildcard?: any [
                not none? find src-str "*"
                not none? find src-str "?"
            ]
            
            either has-wildcard? [
                dir: call-static "System.IO.Path" "GetDirectoryName" reduce [src-str]
                pattern: call-static "System.IO.Path" "GetFileName" reduce [src-str]
                if any [none? dir empty? dir] [ dir: what-dir ]
                if any [none? pattern empty? pattern] [ pattern: "*" ]
                
                dest-dir-exists?: call-static "System.IO.Directory" "Exists" reduce [dest-str]
                if not dest-dir-exists? [
                    throw rejoin ["cp: target '" dest-str "' is not a directory"]
                ]
                
                dir-exists?: call-static "System.IO.Directory" "Exists" reduce [dir]
                either dir-exists? [
                    entries: call-static "System.IO.Directory" "GetFileSystemEntries" reduce [dir pattern]
                    enumerate entries entry [
                        is-file?: call-static "System.IO.File" "Exists" reduce [entry]
                        if is-file? [
                            filename: call-static "System.IO.Path" "GetFileName" reduce [entry]
                            dest-file: call-static "System.IO.Path" "Combine" reduce [dest-str filename]
                            cp-file-helper entry dest-file allow-overwrite is-v
                        ]
                    ]
                ] [
                    print rejoin ["cp: " dir ": No such directory"]
                ]
            ] [
                src-exists-file?: call-static "System.IO.File" "Exists" reduce [src-str]
                either src-exists-file? [
                    dest-exists-dir?: call-static "System.IO.Directory" "Exists" reduce [dest-str]
                    either dest-exists-dir? [
                        filename: call-static "System.IO.Path" "GetFileName" reduce [src-str]
                        dest-file: call-static "System.IO.Path" "Combine" reduce [dest-str filename]
                    ] [
                        dest-file: dest-str
                    ]
                    cp-file-helper src-str dest-file allow-overwrite is-v
                ] [
                    src-exists-dir?: call-static "System.IO.Directory" "Exists" reduce [src-str]
                    either src-exists-dir? [
                        print rejoin ["cp: " src-str " is a directory (not supported)"]
                    ] [
                        print rejoin ["cp: " src-str ": No such file or directory"]
                    ]
                ]
            ]
            none
        ]

        zip: func ["Zips files or folders into an archive." archive sources /force /f /verbose /v /level lvl] [
            is-force: any [:force :f]
            is-verbose: any [:verbose :v]
            comp-level: either level [to-string lvl] ["optimal"]
            
            native-zip archive sources is-force is-verbose comp-level
        ]

        unzip: func ["Extracts the contents of a zip archive." archive dest /force /f /verbose /v] [
            is-force: any [:force :f]
            is-verbose: any [:verbose :v]
            
            native-unzip archive dest is-force is-verbose
        ]

    """;

}