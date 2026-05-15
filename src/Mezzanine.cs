namespace Ragnar;

public static class Mezzanine
{
    public const string SOURCE = """
        does: func [block] [ func [] block ]
        get-env: func [name] [call-static "System.Environment" "GetEnvironmentVariable" reduce [name]]
        max: func [a b] [ either greater? a b [a] [b] ]
        min: func [a b] [ either greater? a b [b] [a] ]
        negate: func [n] [ n * -1 ]
        none?: func [x] [ equal? x none ]
        not-equal?: func [a b] [ not equal? a b ]
        pwd: does [what-dir]
        what-dir: does [call-static "System.IO.Directory" "GetCurrentDirectory" []]
        zero?: func [x] [ x = 0 ]
    """;

}