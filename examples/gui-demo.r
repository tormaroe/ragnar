; Retro Terminal GUI Demo in Ragnar

print "Starting GUI Demo..."

theme-choice: ask "Choose theme (1: retro-terminal [default], 2: classic-rebol, 3: modern-slate, 4: kawaii-blossom): "
either theme-choice = "2" [
    set-theme 'classic-rebol
    print "Theme set to: classic-rebol"
] [
    either theme-choice = "3" [
        set-theme 'modern-slate
        print "Theme set to: modern-slate"
    ] [
        either theme-choice = "4" [
            set-theme 'kawaii-blossom
            print "Theme set to: kawaii-blossom"
        ] [
            set-theme 'retro-terminal
            print "Theme set to: retro-terminal"
        ]
    ]
]

view [
    title "Ragnar Command Center v1.0"
    
    row [
        image "assets/ragnar-logo-small.png" 100
        heading "Ragnar Command Center"
    ]
    
    text "Configure system settings below:"
    
    row [
        text "Subsystem Target:"
        subsystem-choice: choice ["MAIN FRAME" "PROPULSION" "LIFE SUPPORT" "COMMUNICATIONS"] [
            target: get-face subsystem-choice
            set-face status-lbl rejoin ["Target Subsystem selected: " target]
        ]
    ]
    
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
            target: get-face subsystem-choice
            cmd: get-face cmd-field
            level: get-face pwr-slider
            safety: get-face check-override
            
            status-msg: rejoin [
                "Target: " target
                " | Command: " cmd 
                " | Power: " level "%" 
                " | Safety: " either safety ["DISABLED"] ["ENABLED"]
            ]
            set-face status-lbl status-msg
        ]
        
        button "Reset System" [
            set-face subsystem-choice "MAIN FRAME"
            set-face cmd-field "ACTIVATE"
            set-face pwr-slider 75
            set-face check-override false
            set-face status-lbl "System status: STANDBY"
        ]
    ]
]
