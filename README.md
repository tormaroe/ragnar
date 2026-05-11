
![Ragnar logo](assets/ragnar-logo-big.png)

***Ragnar*** is a scripting language inspired by Rebol. It's hosted in and have decent interop with .NET. It is made to be useful from the command line, and have a REPL.

## Status

Hobby project, basically just started.

## Defined functions 

```
>> what

--- Defined Functions ---
add             [native]  append          [native]
call            [native]  call-method     [native]
call-static     [native]  do              [native]
equal?          [native]  exit            [native]
first           [native]  foreach         [native]
func            [native]  get-prop        [native]
get-static      [native]  get-type        [native]
greater?        [native]  help            [native]
if              [native]  join            [native]
last            [native]  length?         [native]
less?           [native]  loop            [native]
mul             [native]  multiply        [native]
new             [native]  print           [native]
probe           [native]  quit            [native]
read            [native]  reduce          [native]
rejoin          [native]  second          [native]
set-prop        [native]  sub             [native]
type?           [native]  what            [native]
while           [native]  write           [native]
```

## File IO

```
file: %somefile.txt
write :file "A"
write/append :file "B"
read :file
```

```
names: read/lines %somefolder/somefile.txt
foreach name names [
    print [name "\n"]
]
```

## TODO

1. String manipulation functions: find, replace, trim, uppercase, lowercase, split, copy
1. Series manipulation functions: copy, select, pick, insert, remove, empty?, reduce, compose, at, next, back, head, tail, reverse, collect, keep, map-each, find