
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


## TODO

1. String manipulation functions: find, replace, uppercase, lowercase, split, copy
1. Series manipulation functions: copy, select, pick, insert, remove, empty?, reduce, compose, at, next, back, head, tail, reverse, collect, keep, map-each, find