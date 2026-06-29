[
    [
        description: "Ragnar evaluates expressions strictly left-to-right (no operator precedence)"
        hint: "1 + 2 evaluates first (3), then 3 * 3 evaluates (9). Try entering 9."
        expression: [__ = (1 + 2 * 3)]
    ]
    [
        description: "Parens can be used to override standard evaluation order"
        hint: "Here, the multiplication runs first because it is in parens. Try entering 7."
        expression: [__ = (1 + (2 * 3))]
    ]
    [
        description: "min returns the lesser of two values"
        hint: "Which is smaller: 10 or 20? Try entering 10."
        expression: [__ = min 10 20]
    ]
    [
        description: "max returns the greater of two values"
        hint: "Which is larger: 10 or 20? Try entering 20."
        expression: [__ = max 10 20]
    ]
    [
        description: "negate negates a number"
        hint: "negate multiplies a number by -1. Try entering -5."
        expression: [__ = negate 5]
    ]
    [
        description: "and returns true only if both operands are truthy"
        hint: "Is true and false true or false? Try entering false."
        expression: [__ = (true and false)]
    ]
    [
        description: "or returns true if at least one operand is truthy"
        hint: "Is true or false true or false? Try entering true."
        expression: [__ = (true or false)]
    ]
    [
        description: "all evaluates a block, returning the last value if all are truthy, or none"
        hint: "All elements are truthy, so the last element is returned. Try entering \"hello\"."
        expression: [__ = all [true 5 "hello"]]
    ]
    [
        description: "any evaluates a block, returning the first truthy value it encounters"
        hint: "any returns the first non-false/non-none value. Try entering 10."
        expression: [__ = any [false none 10 false]]
    ]
]
