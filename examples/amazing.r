; AMAZING PROGRAM in Ragnar
; Based on the classic BASIC maze generator

print "AMAZING PROGRAM"
print "CREATIVE COMPUTING MORRISTOWN, NEW JERSEY"
print ""

; --- SETUP ---

w-str: ask "WIDTH? "
h-str: ask "LENGTH? "

w: to-integer w-str
h: to-integer h-str

if (or (w < 2) (h < 2)) [
    print "Dimensions must be at least 2."
    exit
]

; grid-visited: true if cell is part of the maze
; grid-right: true if there is NO right wall
; grid-bottom: true if there is NO bottom wall

make-grid: func [width height val] [
    grid: copy []
    loop height [
        row: copy []
        loop width [append row val]
        append grid row
    ]
    grid
]

visited: make-grid w h false
no-right: make-grid w h false
no-bottom: make-grid w h false

; Entrance at top
entrance-x: random w
poke (pick visited 1) entrance-x true
visited-count: 1
total-cells: w * h

; --- GENERATION ---

while [visited-count < total-cells] [
    
    ; Find a cell that is visited but has unvisited neighbors
    ; We'll search from a random start point to make it feel more "amazing"
    
    r: random h
    c: random w
    found: false
    
    ; Search loop
    count: 0
    while [all [(not found) (count < total-cells)]] [
        if (pick (pick visited r) c) [
            ; Check neighbors - using 'all' for safety although we only need it for the neighbor check
            if (any [
                all [(c < w) (not (pick (pick visited r) (c + 1)))]
                all [(c > 1) (not (pick (pick visited r) (c - 1)))]
                all [(r < h) (not (pick (pick visited (r + 1)) c))]
                all [(r > 1) (not (pick (pick visited (r - 1)) c))]
            ]) [found: true]
        ]
        
        if (not found) [
            c: c + 1
            if (c > w) [
                c: 1
                r: r + 1
                if (r > h) [r: 1]
            ]
            count: count + 1
        ]
    ]
    
    if (not found) [break] ; Should not happen
    
    ; Now at (c, r) which is in maze and has unvisited neighbors.
    ; Perform a random walk until stuck.
    
    while [true] [
        neighbors: copy []
        if (all [(c < w) (not (pick (pick visited r) (c + 1)))]) [append neighbors "R"]
        if (all [(c > 1) (not (pick (pick visited r) (c - 1)))]) [append neighbors "L"]
        if (all [(r < h) (not (pick (pick visited (r + 1)) c))]) [append neighbors "D"]
        if (all [(r > 1) (not (pick (pick visited (r - 1)) c))]) [append neighbors "U"]
        
        if (equal? 0 (length? neighbors)) [break]
        
        dir: pick neighbors (random (length? neighbors))
        
        if (dir == "R") [
            poke (pick no-right r) c true
            c: c + 1
        ]
        if (dir == "L") [
            c: c - 1
            poke (pick no-right r) c true
        ]
        if (dir == "D") [
            poke (pick no-bottom r) c true
            r: r + 1
        ]
        if (dir == "U") [
            r: r - 1
            poke (pick no-bottom r) c true
        ]
        
        poke (pick visited r) c true
        visited-count: visited-count + 1
    ]
]

; Exit at bottom
exit-x: random w
poke (pick no-bottom h) exit-x true

; --- PRINTING ---

; Top border
line: copy "."
i: 1
while [i <= w] [
    either (equal? i entrance-x) [
        append line "  ."
    ] [
        append line "--."
    ]
    i: i + 1
]
print line

j: 1
while [j <= h] [
    ; Vertical walls (cells and right walls)
    line: copy "I"
    i: 1
    while [i <= w] [
        either (pick (pick no-right j) i) [
            append line "   "
        ] [
            append line "  I"
        ]
        i: i + 1
    ]
    print line
    
    ; Horizontal walls (bottom walls)
    line: copy ":"
    i: 1
    while [i <= w] [
        either (pick (pick no-bottom j) i) [
            append line "  :"
        ] [
            append line "--:"
        ]
        i: i + 1
    ]
    print line
    
    j: j + 1
]

exit 