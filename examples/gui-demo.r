; Retro Terminal GUI Demo in Ragnar

print "Starting Retro GUI Demo..."

view [
    title "Ragnar CRT Terminal v1.0"
    heading "Ragnar Terminal Command Center"
    
    text "Configure system settings below:"
    
    row [
        text "User Command:"
        cmd-field: field "ACTIVATE"
    ]
    
    row [
        text "Power Level:"
        pwr-slider: slider 75
    ]
    
    row [
        check-override: check "Override Safety Protocols" false
    ]
    
    status-lbl: text "System status: STANDBY"
    
    row [
        button "Execute Command" [
            cmd: get-face cmd-field
            level: get-face pwr-slider
            safety: get-face check-override
            
            status-msg: rejoin [
                "Command: " cmd 
                " | Power: " level "%" 
                " | Safety: " either safety ["DISABLED"] ["ENABLED"]
            ]
            set-face status-lbl status-msg
        ]
        
        button "Reset System" [
            set-face cmd-field "ACTIVATE"
            set-face pwr-slider 75
            set-face check-override false
            set-face status-lbl "System status: STANDBY"
        ]
    ]
]
