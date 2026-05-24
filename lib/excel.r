use [get-cell-val wrap-table wrap-range wrap-cell wrap-worksheet wrap-workbook net-new] [
    get-cell-val: func [cell-val] [
        val: none
        either cell-val/IsBlank [
            val: none
        ] [
            either cell-val/IsBoolean [
                val: cell-val/GetBoolean
            ] [
                either cell-val/IsNumber [
                    val: cell-val/GetNumber
                ] [
                    either cell-val/IsDateTime [
                        val: cell-val/GetDateTime
                    ] [
                        val: cell-val/GetText
                    ]
                ]
            ]
        ]
        val
    ]

    wrap-table: func [t] [
        make object! [
            set-theme: func [theme-name] [
                t/Theme: theme-name
            ]
        ]
    ]

    wrap-range: func [r] [
        make object! [
            create-table: func [/with name] [
                table-name: either with [name] [none]
                t: either with [
                    call-method r "CreateTable" [table-name]
                ] [
                    call-method r "CreateTable" []
                ]
                wrap-table t
            ]
        ]
    ]

    wrap-cell: func [c] [
        make object! [
            set-value: func [val] [
                c/Value: val
            ]
            get-value: func [] [
                get-cell-val c/Value
            ]
            insert-data: func [data] [
                r: call-method c "InsertData" [data]
                t: call-method r "CreateTable" []
                wrap-table t
            ]
        ]
    ]

    wrap-worksheet: func [sheet] [
        make object! [
            cell: func [address] [
                wrap-cell call-method sheet "Cell" [address]
            ]
            range: func [address] [
                wrap-range call-method sheet "Range" [address]
            ]
            get-data: func [address /local r rows data row-data cells] [
                r: call-method sheet "Range" [address]
                rows: r/Rows
                data: copy []
                enumerate rows row [
                    row-data: copy []
                    cells: row/Cells
                    enumerate cells cell [
                        append row-data get-cell-val cell/Value
                    ]
                    append data row-data
                ]
                data
            ]
        ]
    ]

    wrap-workbook: func [wb] [
        make object! [
            worksheets: make object! [
                add: func [name] [
                    wrap-worksheet call-method wb/Worksheets "Add" [name]
                ]
                get: func [name] [
                    wrap-worksheet call-method wb "Worksheet" [name]
                ]
            ]
            save-as: func [file] [
                call-method wb "SaveAs" [to-string file]
            ]
        ]
    ]

    net-new: :new
    make object! [
        new: func [] [
            wrap-workbook net-new "ClosedXML.Excel.XLWorkbook" []
        ]
        load: func [file] [
            wrap-workbook net-new "ClosedXML.Excel.XLWorkbook" [to-string file]
        ]
    ]
]