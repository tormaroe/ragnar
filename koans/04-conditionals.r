[
    [
        description: "if evaluates its block only if the condition is truthy"
        hint: "Since true is truthy, the block runs and sets val to 10. Try entering 10."
        expression: [__ = (val: 0 if true [val: 10] val)]
    ]
    [
        description: "if does not evaluate its block if the condition is falsy"
        hint: "Since false is falsy, the block is ignored and val remains 0. Try entering 0."
        expression: [__ = (val: 0 if false [val: 10] val)]
    ]
    [
        description: "either evaluates the first block if true, or the second if false"
        hint: "either behaves like an if-else statement. Try entering \"yes\"."
        expression: [__ = either true ["yes"] ["no"]]
    ]
    [
        description: "either evaluates the second block when the condition is false"
        hint: "The second branch is chosen. Try entering \"no\"."
        expression: [__ = either false ["yes"] ["no"]]
    ]
    [
        description: "In Ragnar, numbers are truthy"
        hint: "Unlike some languages, 5 is considered truthy. Try entering \"yes\"."
        expression: [__ = either 5 ["yes"] ["no"]]
    ]
    [
        description: "Strings (Text) are also truthy"
        hint: "Only false and none are falsy. Try entering \"yes\"."
        expression: [__ = either "hello" ["yes"] ["no"]]
    ]
    [
        description: "The value none is falsy"
        hint: "none behaves as falsy in conditionals. Try entering \"no\"."
        expression: [__ = either none ["yes"] ["no"]]
    ]
    [
        description: "switch selects and evaluates the block matching the key"
        hint: "switch maps a key to a block of code. What is mapped to 'b'? Try entering \"banana\"."
        expression: [__ = switch 'b [a ["apple"] b ["banana"] c ["cherry"]]]
    ]
    [
        description: "switch can take a /default block for unmatched cases"
        hint: "If the case doesn't exist, /default block runs. Try entering \"unknown\"."
        expression: [__ = switch/default 'z [a ["apple"] b ["banana"]] ["unknown"]]
    ]
]
