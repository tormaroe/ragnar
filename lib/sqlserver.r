{
    This library can be used from Ragnar to connect to SQL Server databases and
    execute queries.

    Usage:

    sql: do %lib/sqlserver.r
    conn: sql/connect "Data Source=localhost;Initial Catalog=master;Integrated Security=True"
    result: sql/query conn {
        SELECT name, database_id
        FROM sys.databases
        WHERE state_desc = @state
    } [
        state: "ONLINE"
    ]

    conn/close

    ; result will be a block of blocks, each representing a row from the query result. 

    print ["Row count:" length? result] 

    db1: first result
    == ["name" "master" "database_id" 1]

    print ["Database Name:" select db1 "name" 
           "ID:" select db1 "database_id"]
}

make object! [
    connect: func [connection-string] [
        conn: new "Microsoft.Data.SqlClient.SqlConnection" [connection-string]
        conn/open
        conn
    ]

    query: func [connection query-text parameters] [
        cmd: connection/CreateCommand
        cmd/CommandText: query-text

        if not none? parameters [
            idx: 1
            while [idx <= length? parameters] [
                param-name: pick parameters idx
                param-val: pick parameters (idx + 1)
                clean-name: join "@" replace (to-string param-name) ":" ""
                cmd/Parameters/AddWithValue clean-name param-val
                idx: idx + 2
            ]
        ]

        reader: cmd/ExecuteReader
        result: copy []
        while [reader/Read] [
            row: copy []
            col-idx: 0
            while [col-idx < reader/FieldCount] [
                append row reader/GetName col-idx
                append row reader/GetValue col-idx
                col-idx: col-idx + 1
            ]
            append result to-record row
        ]
        reader/close
        result
    ]
]
