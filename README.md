
![Ragnar logo](assets/ragnar-logo-1200.png)

***Ragnar*** is a programming language made for fun, and carefully vibecoded using Gemini CLI. It is:
- inspired by Rebol. Many core features from **Rebol** are implemented, including the object system.
- unlike Rebol with regards to scoping rules; Ragnar has *lexical scoping*. 
- hosted in and have decent interop with .NET. 
- made to be useful from the command line, and have a REPL. 
- tail call optimized.
- also inspired by **Erlang**, and has a simple actor model implementation.

[![.NET](https://github.com/tormaroe/ragnar/actions/workflows/dotnet.yml/badge.svg)](https://github.com/tormaroe/ragnar/actions/workflows/dotnet.yml)

## Features

### REPL and reflection

TODO

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


### Core Erlang features 

TODO

### .NET interop

TODO

### Object support

TODO

### Tail call optimization

TODO

### Actor model

TODO

### 

TODO
