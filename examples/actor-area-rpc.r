{
    This extends the actor-area example with client-server rpc pattern, also from
    "Programming Erlang" by Joe Armstrong, Chapter 8, "Concurrent Programming".
}
start-area-server: does [
    print "starting area server..."
    spawn [
        forever [
            msg: receive
            client: first msg
            shape: second msg
            reply: :reform >> (partial :tell client)
            switch/default first shape [
                rectangle [
                    reply [ "area of rectangle is" (shape/2 * shape/3) ]
                ]
                circle [
                    reply [ "area of circle is" (3.14159 * (shape/2 * shape/2)) ]
                ]
            ] [
                reply [ "i don't know what the area of a" shape/1 "is." ] 
            ]
        ]
    ]
]

print {
    ;;; To run this example:
    server: start-area-server
    tell server [rectangle 5 10]
    _ print ["Response from server:" second receive]
    tell server [circle 5]
    _ print ["Response from server:" second receive]
    tell server [triangle 5 10]
    _ print ["Response from server:" second receive]
    kill server
}

none