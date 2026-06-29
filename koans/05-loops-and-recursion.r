[
    [
        description: "loop runs a block a specified number of times"
        hint: "loop takes an integer N and runs the block N times. Try entering 5."
        expression: [__ = (count: 0 loop 5 [count: count + 1] count)]
    ]
    [
        description: "while runs a block as long as its condition block is true"
        hint: "The condition [val < 5] is tested. val starts at 1, doubles to 2, 4, then 8. Try entering 8."
        expression: [__ = (val: 1 while [val < 5] [val: val * 2] val)]
    ]
    [
        description: "foreach binds a word to each value in a series and runs a block"
        hint: "We add each number to sum. 1 + 2 + 3 + 4 = 10. Try entering 10."
        expression: [__ = (sum: 0 foreach x [1 2 3 4] [sum: sum + x] sum)]
    ]
    [
        description: "map applies a function to all values in a block"
        hint: "Each value in [1 2 3] is doubled. Try entering [2 4 6]."
        expression: [__ = map func [x] [x * 2] [1 2 3]]
    ]
    [
        description: "filter keeps only the elements that satisfy a predicate"
        hint: "We filter out odd numbers using modulo division (x // 2 = 0). Try entering [2 4 6]."
        expression: [__ = filter func [x] [x // 2 = 0] [1 2 3 4 5 6]]
    ]
    [
        description: "Recursion allows a function to call itself"
        hint: "The factorial of 4 (4 * 3 * 2 * 1) is 24. Try entering 4."
        expression: [24 = (fact: func [n] [either n <= 1 [1] [n * fact (n - 1)]] fact __)]
    ]
]
