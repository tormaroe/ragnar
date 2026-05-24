let [
    get-record-keys func [rec] [
        keys: copy []
        idx: 1
        while [idx <= length? rec] [
            append keys pick rec idx
            idx: idx + 2
        ]
        keys
    ]

    get-record-values func [rec] [
        vals: copy []
        idx: 2
        while [idx <= length? rec] [
            append vals pick rec idx
            idx: idx + 2
        ]
        vals
    ]

    join-with func [delimiter block] [
        res: copy ""
        first-item: true
        foreach item block [
            either first-item [
                res: to-string item
                first-item: false
            ] [
                res: rejoin [res delimiter item]
            ]
        ]
        res
    ]

    pad-cell func [val width is-numeric] [
        s: to-string val
        len: length? s
        pad-spaces: width - len
        padding: copy ""
        while [pad-spaces > 0] [
            append padding " "
            pad-spaces: pad-spaces - 1
        ]
        either is-numeric [
            rejoin [padding s]
        ] [
            rejoin [s padding]
        ]
    ]

    make-table-impl func [data] [
        if empty? data [ return "" ]
        
        headers: none
        rows: copy []
        has-header-sep: false
        
        either record? first data [
            headers: get-record-keys first data
            foreach rec data [
                append rows get-record-values rec
            ]
            has-header-sep: true
        ] [
            headers: first data
            foreach row next data [
                append rows row
            ]
            has-header-sep: false
        ]
        
        col-count: length? headers
        col-widths: copy []
        
        ; Initialize col-widths with header lengths
        foreach h headers [
            append col-widths length? to-string h
        ]
        
        ; Update col-widths with row element lengths
        foreach row rows [
            idx: 1
            while [idx <= col-count] [
                val: pick row idx
                len: length? to-string val
                if len > pick col-widths idx [
                    poke col-widths idx len
                ]
                idx: idx + 1
            ]
        ]
        
        ; Build separator line
        sep-parts: copy []
        foreach w col-widths [
            dashes: copy ""
            ; Each column has L + 2 width in separator (1 padding on each side)
            idx: 1
            while [idx <= (w + 2)] [
                append dashes "-"
                idx: idx + 1
            ]
            append sep-parts dashes
        ]
        sep-line: rejoin ["+" join-with "+" sep-parts "+" to-string #"^/"]
        
        result: copy sep-line
        
        ; If we have records, we print a header row
        either has-header-sep [
            padded-row: copy []
            idx: 1
            foreach h headers [
                append padded-row pad-cell h (pick col-widths idx) false
                idx: idx + 1
            ]
            append result rejoin ["| " join-with " | " padded-row " |" to-string #"^/"]
            append result sep-line
        ] [
            ; For block of blocks, the first block is just a regular row
            padded-row: copy []
            idx: 1
            foreach h headers [
                is-num: any [integer? :h decimal? :h]
                append padded-row pad-cell :h (pick col-widths idx) is-num
                idx: idx + 1
            ]
            append result rejoin ["| " join-with " | " padded-row " |" to-string #"^/"]
        ]
        
        ; Append Data Rows
        foreach row rows [
            padded-row: copy []
            idx: 1
            while [idx <= col-count] [
                val: pick row idx
                is-num: any [integer? :val decimal? :val]
                append padded-row pad-cell :val (pick col-widths idx) is-num
                idx: idx + 1
            ]
            append result rejoin ["| " join-with " | " padded-row " |" to-string #"^/"]
        ]
        
        append result sep-line
        result
    ]
] [
    :make-table-impl
]