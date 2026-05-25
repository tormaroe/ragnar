{
    This example is WORK IN PROGRESS and may not run as-is. 
}

print "--- Initializing Ragnar Audit ---"

; 1. Environment Info
machine:  System.Environment/MachineName
user:     System.Environment/UserName
os:       System.Environment/OSVersion/VersionString
now:      System.DateTime/Now

print ["Auditing Machine:" machine]
print ["User Context:" user]
print ["Current Time:" (call-method now "ToLongTimeString" [])]

; 2. Prepare the Report
report: new "System.Text.StringBuilder" ["AUDIT REPORT^/"]
call-method report "AppendLine" [(rejoin ["OS Version: " os])]
call-method report "AppendLine" ["-------------------------"]

; 3. File System Inspection
files: call-static "System.IO.Directory" "GetFiles" ["."]
print ["Found" (files/Count) "files. Summarizing..."]

enumerate 'file files [
    info: new "System.IO.FileInfo" [file]
    name: info/Name
    size: info/Length
    
    ; Logic and Parentheses Check
    if (greater? size 1024) [
        call-method report "Append" [(rejoin ["LARGE FILE: " name])]
        call-method report "Append" [(rejoin [" (Size: " size " bytes)"])]
        call-method report "AppendLine" [""]
    ]
]

; 4. Meta and Save
meta: [status: "pending" date: none]
meta/status: "complete"
meta/date: (call-method now "ToShortDateString" [])

log-file: %audit-log.txt
write log-file (call-method report "ToString" [])

print ["Audit" meta/status "on" meta/date]
print ["Report saved to:" log-file]