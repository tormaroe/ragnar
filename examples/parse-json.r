;;;
;;; parse-json.r
;;;
;;; Educational example: parse a simple JSON array of flat objects
;;; using Ragnar's parse dialect (inspired by Rebol's parse).
;;;
;;; Supported JSON shapes:
;;;   - Top level: a JSON array  [...]
;;;   - Elements:  flat objects  { "key": value, ... }
;;;   - Value types: string, integer, true, false, null
;;;   (No nested objects or arrays)
;;;
;;; The parse dialect lets us describe the *grammar* of the input
;;; declaratively as a block of rules, rather than writing imperative
;;; scanner code.  Rules are composed with:
;;;
;;;   "literal"          match an exact string
;;;   #"c"               match a single character
;;;   to  "x"            scan forward, stop *before* "x"
;;;   thru "x"           scan forward, stop *after* "x"
;;;   any [rule]         match the rule zero or more times
;;;   opt [rule]         match the rule zero or one times
;;;   [r1 | r2]          alternatives (try r1, then r2)
;;;   copy var [rule]    match rule and copy the consumed text into var
;;;   (expression)       run Ragnar code as a side effect, no input consumed
;;;
;;; Results are collected into a block of flat key-value blocks:
;;;   [ ["name" "Alice" "age" 30 ...] ["name" "Bob" ...] ]
;;;
;;; Use  select obj "key"  to read a field from a parsed object.
;;;

; ---------------------------------------------------------------------------
; Whitespace character rules (used inside parse rules)
; ---------------------------------------------------------------------------

; Individual whitespace characters:
sp:  #" "           ; space
nl:  #"^/"          ; newline  (^/ is the Ragnar/Rebol escape for newline)
cr:  #"^M"          ; carriage return
tab: #"^-"          ; tab

; ws-rule: skip any run of whitespace characters
ws-rule: [ any [sp | nl | cr | tab] ]

; ---------------------------------------------------------------------------
; Helper: coerce a raw captured string to the right Ragnar value type.
;   "true"  -> true
;   "false" -> false
;   "null"  -> none
;   digits  -> integer
;   else    -> string (Text)
; ---------------------------------------------------------------------------

coerce-value: func [raw] [
    raw: trim raw   ; strip surrounding whitespace from the capture

    if raw = "true"  [ return true  ]
    if raw = "false" [ return false ]
    if raw = "null"  [ return none  ]

    ; Try to parse as integer
    if parse raw [ some [#"0" | #"1" | #"2" | #"3" | #"4"
                        | #"5" | #"6" | #"7" | #"8" | #"9"] ] [
        return to-integer raw
    ]

    ; Negative integer
    if parse raw [ #"-" some [#"0" | #"1" | #"2" | #"3" | #"4"
                              | #"5" | #"6" | #"7" | #"8" | #"9"] ] [
        return to-integer raw
    ]

    ; Quoted JSON string — strip the surrounding " characters
    if (length? raw) >= 2 [
        if all [
            (first raw) = #"""
            (last raw)  = #"""
        ] [
            ; Return the content between the quotes
            return trim copy skip raw 1   ; we'll clean up below
        ]
    ]

    raw
]

; Actually, strip-quotes is easier to write as its own helper.
; raw here already has the surrounding quotes removed in parse rules below.
; (We capture just the inner content using  thru {"} ... to {"}  )

; ---------------------------------------------------------------------------
; The main parser function.
; Returns a block of objects, where each object is a flat block:
;   ["name" "Alice" "age" 30 "active" true "score" none]
; Use  select obj "key"  to look up a field.
; ---------------------------------------------------------------------------

parse-json-array: func [input] [

    ; Accumulator: grows with one sub-block per JSON object found
    result: []

    ; Per-object scratch block: collects  "key" value  pairs
    obj-spec: []

    ; Captured raw text for keys and values (set by copy in parse rules)
    raw-key: ""
    raw-val: ""

    ;
    ; --- Grammar rules (defined as named blocks for readability) ---
    ;

    ; A JSON quoted string, inner content only (between the " delimiters).
    ; We use:  thru {"}  to eat the opening quote, then
    ;          copy ... [to {"}]  to grab everything up to the closing quote,
    ;          then  {"}  to consume the closing quote.
    ;
    json-string: [
        thru {"}
        copy raw-key [ to {"} ]
        {"}
    ]

    ; A bare JSON value (integer / true / false / null).
    ; We scan up to the next comma, closing brace, or whitespace
    ; so that coerce-value can detect the type.
    bare-value: [
        copy raw-val [ to ["," | "}" | sp | nl | cr | tab] ]
    ]

    ; A JSON value: either a quoted string (in which case raw-val is set
    ; from raw-key after the string rule fires) or a bare literal.
    ;
    ; Note: after  json-string  fires, raw-key holds the content;
    ; we copy it into raw-val in a paren so later code can use raw-val
    ; uniformly for both cases.
    json-value: [
        [
            thru {"}
            copy raw-val [ to {"} ]
            {"}
        ]
        | bare-value
    ]

    ; One key-value pair:  "key": <value>
    ; Side-effect paren ( ) appends the pair to obj-spec.
    key-value-pair: [
        ws-rule
        thru {"}
        copy raw-key [ to {"} ]
        {"}
        ws-rule ":" ws-rule
        json-value
        (
            ; Append key then coerced value to the flat object accumulator
            append obj-spec raw-key
            append obj-spec coerce-value raw-val
        )
    ]

    ; A complete JSON object:  { pair, pair, ... }
    ; After all pairs are collected we snapshot obj-spec into result.
    json-object: [
        ws-rule "{"
        key-value-pair
        any [ ws-rule "," key-value-pair ]
        ws-rule "}"
        (
            ; Save a copy of this object's data and reset the accumulator
            append/only result copy obj-spec
            obj-spec: []
        )
    ]

    ; Top-level JSON array:  [ obj, obj, ... ]
    json-array: [
        ws-rule "["
        json-object
        any [ ws-rule "," json-object ]
        ws-rule "]"
        ws-rule end
    ]

    ; ------------------------------------------------------------------
    ; Run the parse — returns true if the whole input matched
    ; ------------------------------------------------------------------
    either parse input json-array [
        print "Parsing succeeded!"
    ] [
        print "Parsing FAILED — input does not match the grammar."
    ]

    result
]

; ---------------------------------------------------------------------------
; Sample JSON input
; ---------------------------------------------------------------------------

json: {[
  {"name": "Alice", "age": 30, "active": true,  "score": null},
  {"name": "Bob",   "age": 25, "active": false,  "score": 42},
  {"name": "Carol", "age": 35, "active": true,  "score": 7}
]}

; ---------------------------------------------------------------------------
; Run the parser and display results
; ---------------------------------------------------------------------------

print "^/=== Ragnar parse-json example ==="
print "^/Input JSON:^/"
print json

print "^/--- Running parser ---^/"
objects: parse-json-array json

print ["^/Parsed" length? objects "object(s):^/"]

foreach obj objects [
    ; obj is a flat block like ["name" "Alice" "age" 30 ...]
    ; use  select  to look up values by key
    print rejoin [ "  name   = " select obj "name"   ]
    print rejoin [ "  age    = " select obj "age"    ]
    print rejoin [ "  active = " select obj "active" ]
    print rejoin [ "  score  = " select obj "score"  ]
    print ""
]

none
