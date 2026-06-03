
![Ragnar](assets/repl-logo.png)

***Ragnar*** is a programming language made for fun, and carefully vibecoded using Antigravity. 

- inspired by **Rebol**. Many core features from Rebol are implemented, including the object system and the powerfull parse function.
- hosted in .NET with decent interop. 
- made to be useful from the command line, and have a REPL. 
- has a simple actor model implementation inspired by **Erlang**.
- is functional
    - *lexically scoped*, and all functions are closures (unlike Rebol). 
    - has tail-call optimized recursion (TCO).
    - has functional composition inspired by **F#**.
    - has partial application inspired by **Clojure**.

Ragnar homepage: [tormaroe.github.io/ragnar](https://tormaroe.github.io/ragnar)

[![.NET](https://github.com/tormaroe/ragnar/actions/workflows/dotnet.yml/badge.svg)](https://github.com/tormaroe/ragnar/actions/workflows/dotnet.yml)

---

## Ragnar Quick Reference & Cheat Sheet

### Scoping
- **Hybrid Binding**: Functions evaluate in a local context with a lexical parent (`DefiningContext`) and dynamic parent (caller context).
- **Lookup**: Word resolution traverses the lexical parent chain first, falling back to the dynamic parent chain.
- **Assignment**: Setting a word updates its nearest bound parent context. Inside a function frame, updates do not traverse past the global context, defaulting to local assignment unless forced global.

### Functions, Closures & TCO
- **Closures**: Functions defined via `func` or `does` capture their lexical definition environment.
- **Tail Call Optimization (TCO)**: The interpreter optimizes tail calls by returning a `TailCall` token, resolved iteratively in a trampoline loop.

### REPL Features
- **Special Variable `it`**: Evaluates to the last printed/computed result in the context chain.
- **Auto-completion**: 
  - `Tab` / `Right Arrow`: Cycle forward through context bindings matching the prefix.
  - `Shift + Tab` / `Left Arrow`: Cycle backward.
  - `Escape`: Cancel and restore original input.
  - `Enter`: Accept selection and execute.
- **Navigation & Editing**: `Up`/`Down` (history navigation), `Left`/`Right` (cursor movement), `Home`/`End` (cursor jump), `Backspace`/`Delete`.
- **Persistent History**: Executed commands are saved asynchronously to a `.ragnar_history` file in the user's home directory and loaded on REPL startup (capped at 1,000 entries; filters consecutive duplicates).
- **Customization**: Configure custom behavior at runtime via:
  - `system/console/prompt`: Set a string or evaluation block for prompt.
  - `system/console/result`: Set prefix for evaluated output (defaults to `== `).
  - `system/console/history`: Block storing command line history.
- **Startup Script**: Evaluates `.ragnar.r` in the user's home directory.

### Executing Scripts & Command Line Arguments
- **Unix Shebang Support**: Scripts can start with a hashbang line (e.g., `#!/usr/bin/env ragnar`) and be marked as executable (`chmod +x script.r`) to run them directly.
- **Positional Command Line Invocation**: Execute a script by passing its path directly as the first argument, e.g. `ragnar script.r arg1 arg2`. Ragnar options parsing stops at the script path (or at the `--` separator), allowing script-specific arguments and flags.
- **Accessing Arguments**: Scripts can access the block of arguments passed to them as string (`Text`) values via the `system/options/args` path.

### Visual Dialect (GUI)
- **Interactive GUI**: The `view` function spawns a local HTTP/SSE server, opens the browser, and renders responsive layouts from a Ragnar block. Supported widgets: `heading`, `text`, `field`, `button`, `check`, `slider`, `choice`, `image`, `textarea`, and `spinner`.
- **Dynamic Face Access**: Get or set widget values dynamically using `get-face` and `set-face`.
- **Themes**: Switch active CSS styles using `set-theme` (themes: `'retro-terminal` [default], `'classic-rebol`, `'modern-slate`, `'kawaii-blossom`).

### Core Vocabulary & Functions
All native and mezzanine functions categorized:

- **Visual Dialect (GUI)**: `view`, `get-face`, `set-face`, `set-theme`

- **Constants**: `true`, `false`, `none`
- **Output & Evaluation**: `print`, `prin`, `do`, `probe`
- **Conditionals**: `if`, `either`, `all`, `any`, `case`, `switch`
- **Loops & Iteration**: `loop`, `forever`, `while`, `foreach`, `enumerate`, `map-each`, `map`, `flatmap`, `filter`, `fold`, `break`, `continue`
- **Math & Comparison**: `add`, `+`, `sub`, `-`, `multiply`, `mul`, `*`, `divide`, `/`, `remainder`, `//`, `random`, `abs`, `max`, `min`, `negate`, `zero?`, `greater?`, `>`, `less?`, `<`, `equal?`, `=`, `==`, `not-equal?`, `<>`, `!=`, `greater-or-equal?`, `>=`, `less-or-equal?`, `<=`
- **Logical**: `not`, `and`, `and?`, `or`, `or?`, `xor`, `xor?`
- **Objects & Contexts**: `make`, `in`, `get`, `set`, `bind`, `use`, `let`, `context?`, `object!`, `error!`, `system`
- **Series & Blocks**: `first`, `second`, `next`, `last`, `length?`, `empty?`, `find`, `append`, `join`, `pick`, `select`, `poke`, `index?`, `copy`, `sort`, `reverse`, `back`, `head`, `tail`, `head?`, `tail?`, `clear`, `remove`, `take`, `reduce`, `compose`, `block?`, `paren?`, `record?`
- **Strings & Characters**: `trim`, `replace`, `uppercase`, `lowercase`, `split`, `char?`, `charset`, `text?`, `string?`, `reform`, `rejoin`
- **Control Flow**: `func`, `does`, `return`, `exit`, `quit`, `try`, `attempt`, `catch`, `throw`, `ignore`, `_`
- **Functional Operators**: `>>`, `<<`, `partial`, `|`, `|>`, `>|`, `|>>`, `>|>`, `>>|`
- **IO & File System**: `read`, `write`, `load`, `save`, `cd`, `ls`, `mkdir`, `rmdir`, `rm`, `pushd`, `popd`, `mv`, `cp`, `pwd`, `what-dir`, `ask`, `confirm`, `exists?`
- **.NET Interop**: `get-type`, `new`, `call-method`, `get-prop`, `set-prop`, `get-static`, `call-static`
- **Inspection & OS**: `what`, `help`, `?`, `type?`, `format`, `call` (supports `/pid`), `home`, `get-env`, `set-env`, `list-env`, `now`, `wait`, `rc-file-name`, `proc-status`, `proc-kill`
- **Actors (Erlang-like)**: `spawn`, `tell`, `kill`, `receive`
- **Compression**: `zip`, `unzip`, `native-zip`, `native-unzip`
- **Parsing**: `parse`
