namespace Ragnar;

public static class Mezzanine
{
    public const string SOURCE = """
        does: func ["Defines a function with no arguments." block] [ func [] block ]
        get-env: func ["Returns the value of an environment variable." name] [call-static "System.Environment" "GetEnvironmentVariable" reduce [name]]
        max: func ["Returns the greater of two values." a b] [ either greater? a b [a] [b] ]
        min: func ["Returns the lesser of two values." a b] [ either greater? a b [b] [a] ]
        negate: func ["Returns the negative of a number." n] [ n * -1 ]
        none?: func ["Returns true if the value is none." x] [ equal? x none ]
        not-equal?: func ["Returns true if two values are not equal." a b] [ not equal? a b ]
        pwd: func ["Returns the current working directory."][] [what-dir]
        what-dir: func ["Returns the current working directory."][] [call-static "System.IO.Directory" "GetCurrentDirectory" []]
        zero?: func ["Returns true if the value is zero." x] [ x = 0 ]
    """;

}