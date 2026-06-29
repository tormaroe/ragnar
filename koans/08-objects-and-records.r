[
    [
        description: "Objects are collections of key-value bindings created using make object!"
        hint: "make object! binds words inside its block. What is the value of obj/name? Try entering \"Ragnar\"."
        expression: [__ = (obj: make object! [name: "Ragnar"] obj/name)]
    ]
    [
        description: "to-record converts a block of key-value pairs to a record! datatype"
        hint: "Records can be queried using select. What is select rec 'b? Try entering 20."
        expression: [__ = (rec: to-record [a 10 b 20] select rec 'b)]
    ]
    [
        description: "Fields in objects or records can be accessed using path syntax (slash separation)"
        hint: "The path syntax obj/field looks up the key. Try entering 20."
        expression: [__ = (obj: make object! [x: 10 y: 20] obj/y)]
    ]
    [
        description: "Functions defined inside objects can access the object's local fields directly"
        hint: "The function resolves x from the object's context. Try entering 5."
        expression: [__ = (obj: make object! [x: 5 get-x: does [x]] obj/get-x)]
    ]
    [
        description: "Path syntax can navigate nested objects recursively"
        hint: "obj/info/age traverses into the info object. Try entering 25."
        expression: [__ = (obj: make object! [info: make object! [age: 25]] obj/info/age)]
    ]
]
