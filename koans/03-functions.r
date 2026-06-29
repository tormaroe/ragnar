[
    [
        description: "does defines a parameterless function"
        hint: "does takes a block and returns a function with no arguments. Try entering \"hello\"."
        expression: ["hello" = (say-hello: does [__] say-hello)]
    ]
    [
        description: "func defines a function with arguments"
        hint: "We specify parameters in a block. What value times 2 equals 20? Try entering 10."
        expression: [20 = (double: func [x] [x * 2] double __)]
    ]
    [
        description: "Arguments can be accessed by their parameter names"
        hint: "What value squared is 16? Try entering 4."
        expression: [16 = (square: func [val] [val * val] square __)]
    ]
    [
        description: "Functions can accept multiple parameters"
        hint: "Arguments are passed sequentially. What number plus 5 is 12? Try entering 7."
        expression: [12 = (add-numbers: func [a b] [a + b] add-numbers 5 __)]
    ]
    [
        description: "The return function exits the function early with a specific value"
        hint: "If n > 10, the function returns \"big\". What argument makes it return \"big\"? Try entering 15."
        expression: ["big" = (check-size: func [n] [if n > 10 [return "big"] "small"] check-size __)]
    ]
    [
        description: "The exit function exits the function early returning none"
        hint: "exit has no argument and returns none. What input triggers the exit? Try entering 0."
        expression: [none = (stop-early: func [n] [if n = 0 [exit] "go"] stop-early __)]
    ]
    [
        description: "Local variables can be declared using /local refinement"
        hint: "The y variable is local. x + y is evaluated. Try entering 15 to make 15 + 10 = 25."
        expression: [25 = (local-test: func [x /local y] [y: 10 x + y] local-test __)]
    ]
    [
        description: "Refinements serve as boolean flags to modify function behavior"
        hint: "Calling a function with /refinement sets the refinement variable to true. Try entering 7."
        expression: [__ = (increment: func [x /by-two] [either by-two [x + 2] [x + 1]] increment/by-two 5)]
    ]
    [
        description: "Refinements can also accept their own arguments"
        hint: "Arguments to refinements are specified after /refinement name in the spec. Try entering 5."
        expression: [15 = (custom-add: func [x /with val] [either with [x + val] [x + 1]] custom-add/with 10 __)]
    ]
]
