
![Ragnar logo](assets/ragnar-logo-1200.png)

***Ragnar*** is a programming language made for fun, and carefully vibecoded using Gemini CLI. 

- inspired by **Rebol**. Many core features from Rebol are implemented, including the [object system](#object-support) and the powerfull [parse function](#parse).
- hosted in .NET with [decent interop](#net-interop). 
- made to be useful from the command line, and have a [REPL](#repl-and-reflection). 
- has a simple [actor model](#actor-model) implementation inspired by **Erlang**.
- is [functional](#functional-programming)
    - *lexically scoped*, and all functions are closures (unlike Rebol). 
    - has [tail-call optimized](#tail-call-optimization-toc) recursion (TCO).
    - has functional composition inspired by **F#**.
    - has partial application inspired by **Clojure**.

[![.NET](https://github.com/tormaroe/ragnar/actions/workflows/dotnet.yml/badge.svg)](https://github.com/tormaroe/ragnar/actions/workflows/dotnet.yml)

## Features

### REPL and reflection

Ragnar provides an interactive REPL for immediate evaluation, along with reflective capabilities to inspect types and functions.

```rebol
>> type? 42
== integer!

>> type? "hello"
== text!

>> type? [1 2 3]
== block!

>> help add
WORD: add
TYPE:  Native Function
TITLE: Returns the sum of two values.
ARITY: 2
ARGS:  [ a b ]

>> what
add             Returns the sum of two values.
print           Prints a value to the output.
...

>> probe 10 + 20
30
== 30
```

Functions: `help` (or `?`), `what`, `probe`, `type?`

### Configuration

Ragnar will look for a .ragnar.r file in your HOME directory. Here is how that can be used to configure your REPL:

```rebol 
>> config-path: join home rc-file-name
== %C:\Users\bob/.ragnar.r
>> write config-path mold/only [
..   me: "Bob"
..   print ["Hello" me "it is now" now/time]
..   system/console/prompt: "?? "
.. ]
```

Now when you restart your REPL is will say hello, and the prompt is modified:

```rebol 
Hello Bob it is now 10:53:01 AM
REPL Mode (type 'quit' to exit)
?? me
== "Bob"
??
``` 

You can also reload your configuration file by executing:

```rebol 
do join home rc-file-name
```


### Core Ragnar features 

Ragnar's core syntax is built around words, blocks, assignment, and conditional evaluation.

```rebol
; Variable assignment
name: "Ragnar"
age: 25

; Conditionals
either age > 18 [
    print "Adult"
] [
    print "Minor"
]
; == "Adult"

; Logic combinations (infix and prefix)
all [age > 18 name == "Ragnar"] ; returns true
any [age < 18 name == "Ragnar"] ; returns true

; Foreach and filtering
data: [10 21 30 43 50]
evens: []
foreach n data [
    if (n // 2) == 0 [ append evens n ]
]
; evens is now [10 30 50]

; Series manipulation
pick evens 1   ; returns 10
evens/2        ; returns 30 (path navigation)
select [a 1 b 2] 'b ; returns 2
```

Functions: `if`, `either`, `switch`, `all`, `any`, `foreach`, `while`, `loop`, `forever`, `append`, `insert`, `remove`, `change`, `copy`, `pick`, `poke`, `select`, `sort`, `reverse`, `length?`, `empty?`

### .NET interop

Ragnar has built-in interop with .NET, allowing you to instantiate classes, call methods, get/set properties, and access static members. It also supports standard path navigation to access members cleanly.

```rebol
; Instantiating with new
builder: new "System.Text.StringBuilder" ["Hello"]

; Calling instance methods
call-method builder "Append" [" World"]

; Path navigation (getting property value)
builder/Length ; returns 11

; Path navigation (static member access)
System.Math/PI ; returns 3.141592653589793

; Path navigation (setting property value)
builder/Length: 5
call-method builder "ToString" [] ; returns "Hello"
```

Functions: `get-type`, `new`, `get-prop`, `set-prop`, `call-method`, `get-static`, `call-static`, `get-env`

### Object support

Objects in Ragnar are dynamic contexts containing key-value pairs (bindings). You can access and mutate fields using path notation, and perform dynamic scoping or lookup using `in` and `bind`.

```rebol 
square: make object! [
    side: 0
    area: does [ self/side * self/side ]
    perimeter: does [ 4 * self/side ]
    multiply: func [x] [
        self/side: x * self/side
    ]
]

square/side: 3   ; set side length
square/area      ; returns 9
square/perimeter ; returns 12

; Retrieve word bound to object context
word: in square 'side
get word ; returns 3
```

Functions: `context?`, `bind`, `in`, `make`, `get`, `set`

### Parse

Ragnar features a powerful parsing engine supporting simple string splitting and complex dialect-based pattern matching with backtracking.

```rebol
; 1. Simple delimiter splitting
parse "alice,30,engineer" ","
; == [ "alice" "30" "engineer" ]

; 2. Dialect pattern matching
digits: charset "0123456789"
phone-num: [3 digits "-" 4 digits]
parse "467-8000" phone-num ; returns true
```

Functions: `parse`, `charset`

### Tail-Call Optimization (TOC)

As functional programming often produce elegant solutions Ragnar needs tail-call optimization.

```rebol 
factorial: func [n] [
    loop: func [i accum] [
        either i > n [
            accum
        ] [
            loop (i + 1) (accum * i)  ; Recursion in tail position
        ]
    ]
    loop 1 1 
]

factorial 10  ; 3628800
```

Tail-Call Optimization for Mutual Recursion (trampolining) is also supported. 
Here is an example of a trampoline algorithm to determine if a number is even or odd (don't do this at home, folks):

```rebol 
is-even?: func [n] [
    either n == 1 [ false ] [ is-odd? (n - 1) ]
]
is-odd?: func [n] [
    either n == 1 [ true ] [ is-even? (n - 1) ]
]

is-even? 10001  ; false
is-even? 10002  ; true
```


### Actor model

An actor example inspired by Joe Armstrong of Erlang fame:

```rebol 
start-area-server: does [
    spawn [  ; Starts a new actor process (.NET task)
        forever [
            msg: receive  ; Blocks and waits to receive on a channel
            client: first msg  ; The sender, needs a reply
            shape: second msg
            switch/default first shape [
                rectangle [
                    tell client reform [
                        "area of rectangle is" (shape/2 * shape/3) ]
                ]
                circle [
                    tell client reform [ 
                        "area of circle is" (3.14159 * (shape/2 * shape/2)) ]
                ]
            ] [
                tell client reform [ 
                    "i don't know what the area of a" shape/1 "is." ] 
            ]
        ]
    ]
]

server: start-area-server
print ["Response:" rpc server [rectangle 5 10]]
print ["Response:" rpc server [circle 5]]
print ["Response:" rpc server [triangle 5 10]]
kill server
```

Functions: `kill`, `receive`, `rpc`, `tell`

### Functional programming

Ragnar is a functional language supporting lexical closures, partial application, and function composition.

```rebol
; Lexical closures capturing state
make-counter: func [start] [
    func [] [
        current: start
        start: start + 1
        current
    ]
]
counter: make-counter 10
counter ; returns 10
counter ; returns 11

; Partial application
add-five: partial :add 5
add-five 10 ; returns 15

; Function composition (forward >> and backward <<)
inc: func [n] [n + 1]
double: func [n] [n * 2]

f-forward: :inc >> :double  ; (x + 1) * 2
f-backward: :inc << :double ; (x * 2) + 1

f-forward 5  ; returns 12
f-backward 5 ; returns 11
```

Functions: `>>`, `<<`, `partial`, `func`
