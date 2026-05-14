
![Ragnar logo](assets/ragnar-logo-big.png)

***Ragnar*** is a scripting language inspired by Rebol. It's hosted in and have decent interop with .NET. It is made to be useful from the command line, and have a REPL.

### Status

Hobby project, basically just started.

## Operators

Ragnar supports standard infix operators for math and comparisons. Infix operators have higher precedence than prefix functions (e.g., `add 1 2 * 3` is evaluated as `add 1 (2 * 3)`). Math operators are left-associative.

### Math Operators
- `+`: Addition
- `-`: Subtraction
- `*`: Multiplication
- `/`: Division

### Comparison Operators
- `=`: Equal
- `==`: Strict equal
- `<>`: Not equal
- `!=`: Not equal (alias)
- `<`: Less than
- `>`: Greater than
- `<=`: Less than or equal
- `>=`: Greater than or equal

## Native Function Reference

### `trim`
Removes whitespace from a string. By default, it removes leading and trailing whitespace.

**Arguments:**
- `value` [text]: The string to trim.

**Refinements:**
- `/all`: Removes ALL whitespace from the string.
- `/lines`: Reduces multiple internal whitespaces/newlines to a single space, and trims head/tail.
- `/head`: Removes only leading whitespace.
- `/tail`: Removes only trailing whitespace.

**Examples:**
```rebol
>> trim "  hello  "
"hello"

>> trim/all " h e l l o "
"hello"

>> trim/lines "  hello\n  world  "
"hello world"

>> trim/head "  hello  "
"hello  "
```

### `replace`
Replaces occurrences of a search string with a replacement string.

**Arguments:**
- `target` [text]: The string to modify.
- `search` [text]: The string to look for.
- `replacement` [text]: The string to replace it with.

**Refinements:**
- `/all`: Replaces all occurrences instead of just the first one.

**Examples:**
```rebol
>> replace "banana" "a" "o"
"bonana"

>> replace/all "banana" "a" "o"
"bonono"
```

### `uppercase`
Converts a string to uppercase.

**Arguments:**
- `value` [text]: The string to convert.

**Examples:**
```rebol
>> uppercase "hello"
"HELLO"
```

### `lowercase`
Converts a string to lowercase.

**Arguments:**
- `value` [text]: The string to convert.

**Examples:**
```rebol
>> lowercase "HELLO"
"hello"
```

### `split`
Splits a string into a block of strings based on a delimiter.

**Arguments:**
- `value` [text]: The string to split.
- `delimiter` [text]: The delimiter to split by.

**Examples:**
```rebol
>> split "one,two,three" ","
[ "one" "two" "three" ]
```

### `find`
Finds a value in a series (text or block). Returns the series at the start of the match, or `none`.

**Arguments:**
- `series` [series]: The series to search in.
- `value` [any]: The value to search for.

**Refinements:**
- `/case`: Performs a case-sensitive search.
- `/any`: Enables wildcard matching (`*` for any sequence, `?` for any single character).
- `/last`: Searches from the end of the series.
- `/tail`: Returns the series starting immediately *after* the match.
- `/match`: Only matches if the value is at the current series position.

**Examples:**
```rebol
>> find "abcdef" "cd"
"cdef"

>> find/tail "abcdef" "cd"
"ef"

>> find/case "ABC" "a"
none

>> find/any "abcdef" "a?c"
"abcdef"

>> find/last "banana" "a"
"a"

>> find [a b c d] 'c
[ c d ]
```

### `case`
Evaluates a block of condition-block pairs. It evaluates each condition and executes the block associated with the first one that is true.

**Arguments:**
- `block` [block]: A block containing alternating conditions and action blocks.

**Refinements:**
- `/all`: Evaluates all conditions and executes all corresponding blocks, instead of stopping at the first match. Returns the result of the last executed block.

**Examples:**
```rebol
>> case [
    greater? 1 2 [ "wrong" ]
    less? 1 2    [ "right" ]
]
"right"

>> a: 0
>> case/all [
    true [ a: add a 1 ]
    true [ a: add a 10 ]
]
>> a
11
```

### `ask`
Prompts the user for input with a specific message. Returns the user's response as a `text!` value.

**Arguments:**
- `prompt` [text]: The message to display to the user.

**Examples:**
```rebol
>> name: ask "What is your name? "
What is your name? Torbjørn
== "Torbjørn"
```

### `input`
Reads a line of text from the console. Unlike `ask`, it does not display a prompt.

**Returns:**
- [text]: The text entered by the user.

**Examples:**
```rebol
>> data: input
Hello world
== "Hello world"
```

### `confirm`
Prompts the user for a confirmation from a set of options. 

**Arguments:**
- `question` [text]: The question to ask.

**Refinements:**
- `/with`: Allows specifying custom options. Requires a block with two or more values.

**Returns:**
- If exactly 2 options (including the default `y/n`): Returns `true` for the first option and `false` for the second.
- If more than 2 options: Returns the selected value itself.

**Examples:**
```rebol
>> if confirm "Delete file?" [ print "Deleting..." ]
Delete file? (y/n) y
Deleting...

>> confirm/with "Install?" [ "yes" "no" ]
Install? (yes/no) yes
== true

>> choice: confirm/with "Action?" [ "edit" "delete" "cancel" ]
Action? (edit/delete/cancel) delete
== "delete"
```

## TODO

1. String manipulation functions: copy
1. Series manipulation functions: copy, select, pick, insert, remove, empty?, reduce, compose, at, next, back, head, tail, reverse, collect, keep, map-each, find
