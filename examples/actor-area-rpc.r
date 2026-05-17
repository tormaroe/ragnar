{
    This extends the actor-area example with client-server rpc pattern, also from
    "Programming Erlang" by Joe Armstrong, Chapter 8, "Concurrent Programming".
}
start-area-server: does [
    print "Starting area server..."
    spawn [
        forever [
            msg: receive
            client: first msg
            shape: second msg
            reply: :reform >> (partial :tell client)
            switch/default first shape [
                rectangle [
                    reply [ "Area of rectangle is" (shape/2 * shape/3) ]
                ]
                circle [
                    reply [ "Area of circle is" (* 3.14159 (shape/2 * shape/2)) ]
                ]
            ] [
                reply [ "I don't know what the area of a" shape/1 "is." ] 
            ]
        ]
    ]
]

get-area: func [server shape] [
    rpc server shape
]

print {
    ;;; To run this example:
    server: start-area-server
    print ["Response from server:" rpc server [rectangle 5 10]]
    print ["Response from server:" rpc server [circle 5]]
    print ["Response from server:" rpc server [triangle 5 10]]
    kill server
}

none