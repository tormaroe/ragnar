[
    [
        description: "call-static calls a static method on a .NET type"
        hint: "Math.Sqrt(25.0) calculates the square root of 25.0. Try entering 25.0."
        expression: [5.0 = call-static "System.Math" "Sqrt" [__]]
    ]
    [
        description: "new instantiates a .NET class by type name and constructor arguments"
        hint: "We initialize a StringBuilder with \"hello\", so its initial Length is 5. Try entering 5."
        expression: [__ = (sb: new "System.Text.StringBuilder" ["hello"] get-prop sb "Length")]
    ]
    [
        description: "call-method invokes an instance method on a .NET object"
        hint: "We append \" world\" to the StringBuilder. Try entering \" world\"."
        expression: ["hello world" = (sb: new "System.Text.StringBuilder" ["hello"] call-method sb "Append" [__] call-method sb "ToString" [])]
    ]
    [
        description: "get-prop retrieves the value of a property on a .NET instance"
        hint: "The Length property returns the number of characters in the StringBuilder. Try entering 5."
        expression: [__ = (sb: new "System.Text.StringBuilder" ["hello"] get-prop sb "Length")]
    ]
    [
        description: "get-static retrieves the value of a static field or property on a .NET type"
        hint: "Math.PI returns the value of Pi. Try entering 3.141592653589793."
        expression: [__ = get-static "System.Math" "PI"]
    ]
]
