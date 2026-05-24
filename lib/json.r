; Ragnar JSON Library

let [ragnar-parse :parse] [

    make object! [
        ; --- Parser Stack ---
        stack: copy []
        
        push: func [v] [
            append stack :v
        ]
        
        pop: func [/local v] [
            v: last stack
            remove back tail stack
            :v
        ]
        
        append-to-array: func [/local val arr] [
            val: pop
            arr: pop
            append arr :val
            push arr
        ]
        
        append-to-object: func [/local val key obj] [
            val: pop
            key: pop
            obj: pop
            append obj to-set-word key
            append obj :val
            push obj
        ]
        
        ; --- String Unescaping ---
        unescape: func [s /local res char esc-code val hex-digit] [
            res: copy ""
            hex-digit: charset "0123456789abcdefABCDEF"
            ragnar-parse s [
                any [
                    "\\\"" (append res "\"")
                    | "\\\\" (append res "\\")
                    | "\\/" (append res "/")
                    | "\\b" (append res to-char 8)
                    | "\\f" (append res to-char 12)
                    | "\\n" (append res to-char 10)
                    | "\\r" (append res to-char 13)
                    | "\\t" (append res to-char 9)
                    | "\\u" copy esc-code 4 hex-digit (
                        val: call-static "System.Convert" "ToInt32" reduce [esc-code 16]
                        append res to-char val
                    )
                    | copy char skip (append res char)
                ]
            ]
            res
        ]
        
        ; --- JSON Parse Rules ---
        digit: charset "0123456789"
        space: charset reduce [#" " to-char 10 to-char 9 to-char 13]
        spaces: [any space]
        
        json-value: [
            spaces
            [
                ; String
                #"^"" copy str-val [any [#"\" skip | not #"^"" skip]] #"^"" (push unescape str-val)
                
                ; Number
                | copy num-val [
                    opt #"-" 
                    [#"0" | some digit] 
                    opt [#"." some digit] 
                    opt [[#"e" | #"E"] opt [#"+" | #"-"] some digit]
                ] (
                    push either any [find num-val "." find num-val "e" find num-val "E"] [to-decimal num-val] [to-integer num-val]
                )
                
                ; Object
                | #"{" (push to-record []) spaces opt [
                    #"^"" copy k-val [any [#"\" skip | not #"^"" skip]] #"^"" (push unescape k-val)
                    spaces #":" json-value (append-to-object)
                    any [
                        spaces #"," spaces 
                        #"^"" copy k-val [any [#"\" skip | not #"^"" skip]] #"^"" (push unescape k-val)
                        spaces #":" json-value (append-to-object)
                    ]
                ] spaces #"}"
                
                ; Array
                | #"[" (push copy []) spaces opt [
                    json-value (append-to-array)
                    any [spaces #"," json-value (append-to-array)]
                ] spaces #"]"
                
                ; Booleans & Null
                | "true" (push true)
                | "false" (push false)
                | "null" (push none)
            ]
            spaces
        ]
        
        parse: func [json-str] [
            clear stack
            either ragnar-parse json-str [json-value] [
                pop
            ] [
                make error! "Invalid JSON string"
            ]
        ]
    
        ; --- JSON Stringification ---
        
        escape-string: func [s /local res char val hex] [
            res: copy "\""
            foreach char s [
                val: to-integer to-char char
                switch/default val [
                    34 [ append res "\\\"" ]
                    92 [ append res "\\\\" ]
                    10 [ append res "\\n" ]
                    13 [ append res "\\r" ]
                    9  [ append res "\\t" ]
                    8  [ append res "\\b" ]
                    12 [ append res "\\f" ]
                ] [
                    either val < 32 [
                        hex: call-static "System.String" "Format" reduce ["\\u{0:x4}" val]
                        append res hex
                    ] [
                        append res char
                    ]
                ]
            ]
            append res "\""
            res
        ]
    
        stringify-val: func [val indent /local newline-str next-indent res first-item idx k v item] [
            newline-str: either none? indent [""] [to-string #"^/"]
            next-indent: either none? indent [none] [rejoin [indent "    "]]
            
            either record? :val [
                either empty? val [
                    return "{}"
                ] [
                    res: rejoin ["{" newline-str]
                    first-item: true
                    idx: 1
                    while [idx <= length? val] [
                        k: pick val idx
                        v: pick val (idx + 1)
                        
                        either first-item [
                            first-item: false
                        ] [
                            append res rejoin ["," newline-str]
                        ]
                        
                        if not none? next-indent [
                            append res next-indent
                        ]
                        
                        append res rejoin ["\"" to-string k "\":"]
                        if not none? next-indent [
                            append res " "
                        ]
                        append res stringify-val :v next-indent
                        
                        idx: idx + 2
                    ]
                    append res rejoin [newline-str either none? indent [""] [indent] "}"]
                    res
                ]
            ] [
                either block? :val [
                    either empty? val [
                        return "[]"
                    ] [
                        res: rejoin ["[" newline-str]
                        first-item: true
                        foreach item val [
                            either first-item [
                                first-item: false
                            ] [
                                append res rejoin ["," newline-str]
                            ]
                            if not none? next-indent [
                                append res next-indent
                            ]
                            append res stringify-val :item next-indent
                        ]
                        append res rejoin [newline-str either none? indent [""] [indent] "]"]
                        res
                    ]
                ] [
                    either string? :val [
                        escape-string val
                    ] [
                        either logic? :val [
                            either val ["true"] ["false"]
                        ] [
                            either none? :val [
                                "null"
                            ] [
                                to-string val
                            ]
                        ]
                    ]
                ]
            ]
        ]
    
        stringify: func [val /pretty /local indent] [
            indent: either pretty [copy ""] [none]
            stringify-val :val indent
        ]
    ]
]