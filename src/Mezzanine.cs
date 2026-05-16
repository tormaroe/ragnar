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
        get-env: func ["Returns the value of an environment variable." name] [call-static "System.Environment" "GetEnvironmentVariable" reduce [name]]
        max: func ["Returns the greater of two values." a b] [ either greater? a b [a] [b] ]
        min: func ["Returns the lesser of two values." a b] [ either greater? a b [b] [a] ]
        negate: func ["Returns the negative of a number." n] [ n * -1 ]
        none?: func ["Returns true if the value is none." x] [ equal? x none ]
        not-equal?: func ["Returns true if two values are not equal." a b] [ not equal? a b ]
        pwd: func ["Returns the current working directory."] [what-dir]
        rejoin: func ["Reduces and joins a block of values into a string." block] [
            join "" reduce block
        ]
        what-dir: func ["Returns the current working directory."] [call-static "System.IO.Directory" "GetCurrentDirectory" []]
        zero?: func ["Returns true if the value is zero." x] [ x = 0 ]
    """;

}