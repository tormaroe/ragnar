{
    A Simple Example - Ragnar implementation of example from
    "Programming Erlang" by Joe Armstrong, Chapter 8, "Concurrent Programming".
}
start-area-server: does [
    print "Starting area server..."
    spawn [
        forever [
            msg: receive
            switch/default first msg [
                rectangle [
                    print [ "Area of rectangle is" (msg/2 * msg/3)]
                ]
                circle [
                    print [ "Area of circle is" (* 3.14159 (msg/2 * msg/2))]
                ]
            ] [
                print [ "I don't know what the area of a" msg/1 "is." ] 
            ]
        ]
    ]
]

print {
    ;;; To run this example:
    server: start-area-server
    tell server [rectangle 5 10]
    tell server [circle 5]
    tell server [triangle 5 10]
    wait 1000
    kill server
}

none