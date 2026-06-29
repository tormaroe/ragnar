[
    [
        description: "Characters in Ragnar are represented using the #\"char\" syntax"
        hint: "A character literal starts with # followed by a double-quoted character. Try entering #\"a\"."
        expression: [__ = #"a"]
    ]
    [
        description: "text? and string? return true if a value is text"
        hint: "Strings are wrapped in double quotes. Try entering \"hello\"."
        expression: [text? __]
    ]
    [
        description: "Strings are series and have a measurable length"
        hint: "length? returns the number of characters in a string. Try entering 3."
        expression: [__ = length? "abc"]
    ]
    [
        description: "Positional series access on a string returns a single-character string"
        hint: "first of \"abc\" returns the first character as a text value. Try entering \"a\"."
        expression: [__ = first "abc"]
    ]
    [
        description: "uppercase converts a string to all uppercase characters"
        hint: "uppercase returns the uppercase string. Try entering \"ABC\"."
        expression: [__ = uppercase "abc"]
    ]
    [
        description: "lowercase converts a string to all lowercase characters"
        hint: "lowercase returns the lowercase string. Try entering \"abc\"."
        expression: [__ = lowercase "ABC"]
    ]
    [
        description: "join concatenates two string/text values together"
        hint: "join concatenates the strings. Try entering \"hello world\"."
        expression: [__ = join "hello" " world"]
    ]
    [
        description: "rejoin joins a block of strings into a single string"
        hint: "rejoin concatenates all block values. Try entering \"abc\"."
        expression: [__ = rejoin ["a" "b" "c"]]
    ]
    [
        description: "reform joins a block of values with spaces in between"
        hint: "reform reduces the block and adds spaces. Try entering \"a b c\"."
        expression: [__ = reform ["a" "b" "c"]]
    ]
]
