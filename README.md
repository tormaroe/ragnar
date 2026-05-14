
![Ragnar logo](assets/ragnar-logo-big.png)

***Ragnar*** is a scripting language inspired by Rebol. It's hosted in and have decent interop with .NET. It is made to be useful from the command line, and have a REPL.

### Status

Hobby project, basically just started.

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

## TODO

1. String manipulation functions: copy
1. Series manipulation functions: copy, select, pick, insert, remove, empty?, reduce, compose, at, next, back, head, tail, reverse, collect, keep, map-each, find
