namespace Ragnar;

public static class Mezzanine
{
    public const string SOURCE = """
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
        what-dir: func ["Returns the current working directory."] [call-static "System.IO.Directory" "GetCurrentDirectory" []]
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

    """;

}