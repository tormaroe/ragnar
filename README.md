
![Ragnar logo](assets/ragnar-logo-1200.png)

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
        - [`remainder`](#remainder)
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
    - [File IO](#file-io)
        - [`read`](#read)
        - [`write`](#write)
    - [Input & Output](#input--output)
        - [`ask`](#ask)
        - [`confirm`](#confirm)
        - [`input`](#input)
        - [`print`](#print)
    - [Inspection](#inspection)
        - [`help`](#help)
        - [`probe`](#probe)
        - [`type?`](#type)
        - [`what`](#what)
    - [Looping](#looping)
        - [`foreach`](#foreach)
        - [`loop`](#loop)
        - [`while`](#while)
    - [.NET Interop](#net-interop)
        - [Path Navigation](#path-navigation)
        - [`call-method`](#call-method)
        - [`call-static`](#call-static)
        - [`get-prop`](#get-prop)
        - [`get-static`](#get-static)
        - [`get-type`](#get-type)
        - [`new`](#new)
        - [`set-prop`](#set-prop)
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
    - [System & OS](#system--os)
        - [`call`](#call)
- [TODO](#todo)

## Operators

Ragnar supports standard infix operators for math and comparisons. Infix operators have higher precedence than prefix functions (e.g., `add 1 2 * 3` is evaluated as `add 1 (2 * 3)`). Math operators are left-associative.

### Math Operators
- `+`: Addition
- `-`: Subtraction
- `*`: Multiplication
- `/`: Division
- `//`: Remainder

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

#### `remainder`
Returns the remainder of the division of two values.

**Arguments:**
- `value1` [number]
- `value2` [number]

**Examples:**
```rebol
>> remainder 10 3
1
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

### File IO

#### `read`
Reads the content of a file.

**Arguments:**
- `source` [file! or text!]: The path to the file.

**Refinements:**
- `/lines`: Returns a block of strings, one for each line in the file.

**Examples:**
```rebol
>> text: read %data.txt
>> lines: read/lines %data.txt
```

#### `write`
Writes data to a file.

**Arguments:**
- `target` [file! or text!]: The path to the file.
- `data` [any]: The data to write (will be converted to string).

**Refinements:**
- `/append`: Appends the data to the end of the file instead of overwriting.

**Examples:**
```rebol
>> write %log.txt "Session started"
>> write/append %log.txt " - Event 1"
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

### Inspection

#### `help`
Displays help information for a word.

**Arguments:**
- `word` [word]: The word to look up.

**Examples:**
```rebol
>> help print
WORD: print
TYPE: Native Function
ARITY: 1 arguments
```

#### `probe`
Prints the internal representation of a value and returns the value itself. Useful for debugging within expressions.

**Arguments:**
- `value` [any]

**Examples:**
```rebol
>> a: probe add 1 2
3
== 3
```

#### `type?`
Returns the type of a value as a word (e.g., `integer!`, `text!`).

**Arguments:**
- `value` [any]

**Examples:**
```rebol
>> type? 10
integer!
```

#### `what`
Lists all defined functions in the current context.

**Examples:**
```rebol
>> what
--- Defined Functions ---
add             [native]  
ask             [native]  
...
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

### .NET Interop

Ragnar provides deep integration with the .NET runtime. You can instantiate classes, call methods, and access properties using both native functions and idiomatic path navigation.

#### Path Navigation

You can use the slash syntax (`/`) to access properties and fields of .NET objects or static types. This is often cleaner than using `get-prop` or `get-static`.

**Instance Access:**
```rebol
>> now: get-static "System.DateTime" "Now"
>> print now/Year
2026
```

**Static Access:**
If a word is not defined in the Ragnar context, the interpreter will try to resolve it as a .NET type name.
```rebol
>> print System.Math/PI
3.141592653589793
```

**Setting Properties:**
You can use the set-path syntax (colon suffix) to set properties or fields.
```rebol
>> sb: new "System.Text.StringBuilder" []
>> sb/Capacity: 100
>> print sb/Capacity
100
```

**Case Insensitivity:**
Ragnar's .NET interop is case-insensitive by default.
```rebol
>> now/year  ; same as now/Year
```

#### `call-method`
Calls an instance method on a .NET object.

**Arguments:**
- `object` [dotnet!]: The .NET instance.
- `name` [text]: The name of the method.
- `args` [block]: A block of arguments to pass to the method.

**Examples:**
```rebol
>> sb: new "System.Text.StringBuilder" [ "Hello" ]
>> call-method sb "Append" [ " World" ]
>> print sb
Hello World
```

#### `call-static`
Calls a static method on a .NET type.

**Arguments:**
- `type` [text]: The full name of the .NET type.
- `name` [text]: The name of the static method.
- `args` [block]: A block of arguments.

**Examples:**
```rebol
>> call-static "System.Math" "Sqrt" [ 25.0 ]
5.0
```

#### `get-prop`
Gets the value of a property from a .NET object.

**Arguments:**
- `object` [dotnet!]
- `name` [text]: Property name.

**Examples:**
```rebol
>> sb: new "System.Text.StringBuilder" [ "Hi" ]
>> get-prop sb "Length"
2
```

#### `get-static`
Gets the value of a static property or field.

**Arguments:**
- `type` [text]: Full type name.
- `name` [text]: Property or field name.

**Examples:**
```rebol
>> get-static "System.DateTime" "Now"
2026-05-14 14:30:00
```

#### `get-type`
Resolves a .NET type name into a type object.

**Arguments:**
- `name` [text]: Full type name.

**Examples:**
```rebol
>> t: get-type "System.Int32"
```

#### `new`
Instantiates a new .NET object.

**Arguments:**
- `type` [text or dotnet!]: The type name or type object.
- `args` [block]: Constructor arguments.

**Examples:**
```rebol
>> list: new "System.Collections.Generic.List`1[System.String]" []
```

#### `set-prop`
Sets the value of a property on a .NET object.

**Arguments:**
- `object` [dotnet!]
- `name` [text]: Property name.
- `value` [any]

**Examples:**
```rebol
>> sb: new "System.Text.StringBuilder" []
>> set-prop sb "Capacity" 100
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

### System & OS

#### `call`
Executes a system command.

**Arguments:**
- `command` [text]: The command to execute.

**Refinements:**
- `/wait`: Waits for the command to finish and returns the exit code.

**Examples:**
```rebol
>> call "dir"
>> code: call/wait "git status"
```

## TODO

1. String manipulation functions: copy
1. Series manipulation functions: copy, select, pick, insert, remove, empty?, compose, at, next, back, head, tail, reverse, collect, keep, map-each
