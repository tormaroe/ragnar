;;;
;;; Sum all mutiples of 3 or 5 below 1000
;;;     to test the Ragnar language.
;;;

; Solution 1: Function and while loop

f: func [limit] [
     sum: 0
     i:   1
     while [ i < limit ] [
       if (i // 3 = 0) or (i // 5 = 0) [
         sum: sum + i
       ]
       i: i + 1
     ]
     sum
   ]

limit:  1000
answer: f limit
print ["Solution 1: The answer is" answer]


; Solution 2: Recursive list eater

include?: func [x] [ (x // 3 = 0) or (x // 5 = 0) ]

f: func [limit] [
     inner: func [acc n] [
	      either n > 0 [
	        if include? n [
	          acc: acc + n
          ]
	        inner acc (n - 1)
	      ] [
          acc
	      ]
     ]
     inner 0 (limit - 1)
   ]

print ["Solution 2: The answer is" f 1000]

none