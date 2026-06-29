[
    [
        description: "word? checks if a value is any word type (word!, lit-word!, set-word!, get-word!)"
        hint: "A lit-word starts with a single quote. Try entering 'hello."
        expression: [word? '__]
    ]
    [
        description: "A lit-word evaluates to itself as a word value"
        hint: "A lit-word is prefixed with a single quote (e.g. 'foo). Try entering 'foo."
        expression: [(type? first [__]) = 'lit-word!]
    ]
    [
        description: "A set-word has a trailing colon and is used for assignment"
        hint: "A set-word is suffixed with a colon (e.g. foo:). Try entering foo:."
        expression: [(type? first [__]) = 'set-word!]
    ]
    [
        description: "A get-word has a leading colon and retrieves a word's value without evaluation"
        hint: "A get-word is prefixed with a colon (e.g. :foo). Try entering :foo."
        expression: [(type? first [__]) = 'get-word!]
    ]
    [
        description: "set dynamically binds a value to a word, and get retrieves it"
        hint: "set takes a lit-word/word and a value. get retrieves the value. Try entering 42."
        expression: [__ = (set 'my-var 42 get 'my-var)]
    ]
    [
        description: "A get-word retrieves a function value itself instead of invoking it"
        hint: "Evaluating a word bound to a function invokes it, but get-word :word returns the function. Try entering ask."
        expression: [(type? :__) = 'function!]
    ]
]
