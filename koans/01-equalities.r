[
    [
        description: "We shall contemplate truth by testing reality"
        hint: "Everything is equal to itself. Try entering 'true'"
        expression: [equal? true __]
    ]
    [
        description: "To be double sure, let's test reality again"
        hint: "True is true. What is equal to true?"
        expression: [__ = true]
    ]
    [
        description: "We can test reality's opposite"
        hint: "Not true is false."
        expression: [false = (not __)]
    ]
    [
        description: "With help from other values"
        hint: "Not false is true."
        expression: [__ = (not false)]
    ]
    [
        description: "Some things may look different, but be the same"
        hint: "Does 2 equal 2?"
        expression: [__ = (2 = 2)]
    ]
    [
        description: "Indeed, some things are not equal at all"
        hint: "In Ragnar, different types or representations are not equal. 2 is an integer, \"2\" is text. Try \"2\" or 3."
        expression: [2 != __]
    ]
]
