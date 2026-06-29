[
    [
        description: "The first element of a block can be retrieved by first or pick"
        hint: "first retrieves the item at the start of a series. Try entering 10."
        expression: [10 = first [__ 20 30]]
    ]
    [
        description: "Similarly, second retrieves the second element in a series"
        hint: "second retrieves the item at index 2. Try entering 20."
        expression: [20 = second [10 __ 30]]
    ]
    [
        description: "You can pick elements at any specific 1-based index"
        hint: "What index is 30 at in the block? Try entering 3."
        expression: [30 = pick [10 20 30 40] __]
    ]
    [
        description: "The final element can be retrieved with last"
        hint: "last gets the last item. Try entering \"cherry\"."
        expression: ["cherry" = last ["apple" "banana" __]]
    ]
    [
        description: "We can query the length of a series"
        hint: "What is the length of a block with three items? Try entering 3."
        expression: [__ = length? [a b c]]
    ]
    [
        description: "An empty series has a length of zero"
        hint: "empty? checks for emptiness. What is the value that is empty? Try entering []."
        expression: [empty? __]
    ]
    [
        description: "next advances the series pointer to the next position"
        hint: "Evaluating next returns the series from index 2 onwards. What is first of next? Try entering 20."
        expression: [__ = first next [10 20 30]]
    ]
    [
        description: "back moves the series pointer back by one position"
        hint: "If we find 'b', we are at [b c]. If we go back, we are back to [a b c]. Try entering \"a\"."
        expression: [__ = first back find ["a" "b" "c"] "b"]
    ]
    [
        description: "at navigates directly to a 1-based index"
        hint: "at series index returns the series at that position. What index is 30 at? Try entering 3."
        expression: [30 = first at [10 20 30 40] __]
    ]
    [
        description: "head returns the series at its absolute beginning"
        hint: "Even if we find 'c' [c d], head returns the full series starting from the beginning. Try entering 10."
        expression: [__ = first head find [10 20 30 40] 30]
    ]
]
