
![Ragnar logo](assets/ragnar-logo-big.png)

***Ragnar*** is a scripting language inspired by Rebol. It's hosted in and have decent interop with .NET. It is made to be useful from the command line, and have a REPL.

### Status

Hobby project, basically just started.

## Contents

- [Operators](#operators)
- [Native Function Reference](#native-function-reference)
    - [Arithmetic](#arithmetic)
        - [`add`](#add)
        - [`divide`](#divide)
        - [`mul`](#mul)
        - [`multiply`](#multiply)
        - [`sub`](#sub)
    - [Comparison](#comparison)
        - [`equal?`](#equal)
        - [`greater-or-equal?`](#greater-or-equal)
        - [`greater?`](#greater)
        - [`less-or-equal?`](#less-or-equal)
        - [`less?`](#less)
        - [`not-equal?`](#not-equal)
    - [Conditional Logic](#conditional-logic)
        - [`case`](#case)
        - [`if`](#if)
    - [Core Functions](#core-functions)
        - [`do`](#do)
        - [`exit`](#exit)
        - [`func`](#func)
        - [`quit`](#quit)
        - [`reduce`](#reduce)
    - [Input & Output](#input--output)
        - [`ask`](#ask)
        - [`confirm`](#confirm)
        - [`input`](#input)
        - [`print`](#print)
    - [Looping](#looping)
        - [`foreach`](#foreach)
        - [`loop`](#loop)
        - [`while`](#while)
    - [Series & Searching](#series--searching)
        - [`append`](#append)
        - [`find`](#find)
        - [`first`](#first)
        - [`join`](#join)
        - [`last`](#last)
        - [`length?`](#length)
        - [`rejoin`](#rejoin)
        - [`second`](#second)
    - [String Manipulation](#string-manipulation)
        - [`lowercase`](#lowercase)
        - [`replace`](#replace)
        - [`split`](#split)
        - [`trim`](#trim)
        - [`uppercase`](#uppercase)
- [TODO](#todo)

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

### Arithmetic

#### `add`
Returns the sum of two values.

**Arguments:**
- `value1` [number]
- `value2` [number]

**Examples:**
```rebol
>> add 1 2
3
```

#### `divide`
Returns the quotient of two values.

**Arguments:**
- `value1` [number]
- `value2` [number]

**Examples:**
```rebol
>> divide 12 3
4
```

#### `mul`
Alias for `multiply`.

#### `multiply`
Returns the product of two values.

**Arguments:**
- `value1` [number]
- `value2` [number]

**Examples:**
```rebol
>> multiply 3 4
12
```

#### `sub`
Returns the difference of two values.

**Arguments:**
- `value1` [number]
- `value2` [number]

**Examples:**
```rebol
>> sub 10 4
6
```

### Comparison

#### `equal?`
Returns `true` if two values are equal.

**Arguments:**
- `value1` [any]
- `value2` [any]

**Examples:**
```rebol
>> equal? 1 1
true
```

#### `greater-or-equal?`
Returns `true` if the first value is greater than or equal to the second.

**Arguments:**
- `value1` [number]
- `value2` [number]

**Examples:**
```rebol
>> greater-or-equal? 2 2
true
```

#### `greater?`
Returns `true` if the first value is greater than the second.

**Arguments:**
- `value1` [number]
- `value2` [number]

**Examples:**
```rebol
>> greater? 2 1
true
```

#### `less-or-equal?`
Returns `true` if the first value is less than or equal to the second.

**Arguments:**
- `value1` [number]
- `value2` [number]

**Examples:**
```rebol
>> less-or-equal? 1 2
true
```

#### `less?`
Returns `true` if the first value is less than the second.

**Arguments:**
- `value1` [number]
- `value2` [number]

**Examples:**
```rebol
>> less? 1 2
true
```

#### `not-equal?`
Returns `true` if two values are not equal.

**Arguments:**
- `value1` [any]
- `value2` [any]

**Examples:**
```rebol
>> not-equal? 1 2
true
```

### Conditional Logic

#### `case`
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

#### `if`
Evaluates a block if a condition is true.

**Arguments:**
- `condition` [logic]
- `body` [block]

**Examples:**
```rebol
>> if 1 < 2 [ print "Yes!" ]
Yes!
```

### Core Functions

#### `do`
Evaluates a block or returns a value if it is not a block.

**Arguments:**
- `value` [any]

**Examples:**
```rebol
>> do [ add 1 2 ]
3
```

#### `exit`
Exits the Ragnar session.

#### `func`
Defines a new user function.

**Arguments:**
- `spec` [block]: The parameter list.
- `body` [block]: The function body.

**Examples:**
```rebol
>> say-hi: func [ name ] [ rejoin [ "Hello " name "!" ] ]
>> say-hi "Ragnar"
"Hello Ragnar!"
```

#### `quit`
Alias for `exit`.

#### `reduce`
Evaluates all expressions within a block and returns a new block with the results.

**Arguments:**
- `block` [block]

**Examples:**
```rebol
>> reduce [ 1 + 2 3 * 4 ]
[ 3 12 ]
```

### Input & Output

#### `ask`
Prompts the user for input with a specific message. Returns the user's response as a `text!` value.

**Arguments:**
- `prompt` [text]: The message to display to the user.

**Examples:**
```rebol
>> name: ask "What is your name? "
What is your name? Torbjørn
== "Torbjørn"
```

#### `confirm`
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

#### `input`
Reads a line of text from the console. Unlike `ask`, it does not display a prompt.

**Returns:**
- [text]: The text entered by the user.

**Examples:**
```rebol
>> data: input
Hello world
== "Hello world"
```

#### `print`
Prints a value to the console. If the value is a block, it is reduced and joined with spaces before printing.

**Arguments:**
- `value` [any]

**Examples:**
```rebol
>> print "Hello"
Hello
>> print [ "1 + 2 =" 1 + 2 ]
1 + 2 = 3
```

### Looping

#### `foreach`
Iterates over a series, assigning each element to a word and evaluating a body block.

**Arguments:**
- `word` [word]: The variable name (not evaluated).
- `series` [block]: The series to iterate over.
- `body` [block]: The code to execute for each element.

**Examples:**
```rebol
>> foreach x [1 2 3] [ print x * 10 ]
10
20
30
```

#### `loop`
Evaluates a body block a specified number of times.

**Arguments:**
- `count` [integer]
- `body` [block]

**Examples:**
```rebol
>> loop 3 [ print "Hi" ]
Hi
Hi
Hi
```

#### `while`
Repeatedly evaluates a body block as long as a condition block evaluates to true.

**Arguments:**
- `condition` [block]
- `body` [block]

**Examples:**
```rebol
>> n: 0 while [ n < 3 ] [ print n n: n + 1 ]
0
1
2
```

### Series & Searching

#### `append`
Appends a value to the end of a block.

**Arguments:**
- `series` [block]
- `value` [any]

**Examples:**
```rebol
>> b: [1 2] append b 3
[ 1 2 3 ]
```

#### `find`
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

#### `first`
Returns the first value in a series at its current index.

**Arguments:**
- `series` [series]

**Examples:**
```rebol
>> first [10 20 30]
10
```

#### `join`
Concatenates two values. Currently implemented as string concatenation.

**Arguments:**
- `base` [any]
- `value` [any]

**Examples:**
```rebol
>> join "Hello " "World"
"Hello World"
```

#### `last`
Returns the last value in a series.

**Arguments:**
- `series` [series]

**Examples:**
```rebol
>> last [10 20 30]
30
```

#### `length?`
Returns the number of elements in a series from its current index to the end.

**Arguments:**
- `series` [series]

**Examples:**
```rebol
>> length? [1 2 3]
3
```

#### `rejoin`
Evaluates all expressions in a block and joins the results into a single string.

**Arguments:**
- `block` [block]

**Examples:**
```rebol
>> name: "Ragnar" rejoin [ "Hello " name "!" ]
"Hello Ragnar!"
```

#### `second`
Returns the second value in a series relative to its current index.

**Arguments:**
- `series` [series]

**Examples:**
```rebol
>> second [10 20 30]
20
```

### String Manipulation

#### `lowercase`
Converts a string to lowercase.

**Arguments:**
- `value` [text]: The string to convert.

**Examples:**
```rebol
>> lowercase "HELLO"
"hello"
```

#### `replace`
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

#### `split`
Splits a string into a block of strings based on a delimiter.

**Arguments:**
- `value` [text]: The string to split.
- `delimiter` [text]: The delimiter to split by.

**Examples:**
```rebol
>> split "one,two,three" ","
[ "one" "two" "three" ]
```

#### `trim`
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

#### `uppercase`
Converts a string to uppercase.

**Arguments:**
- `value` [text]: The string to convert.

**Examples:**
```rebol
>> uppercase "hello"
"HELLO"
```

## TODO

1. String manipulation functions: copy
1. Series manipulation functions: copy, select, pick, insert, remove, empty?, reduce, compose, at, next, back, head, tail, reverse, collect, keep, map-each, find
