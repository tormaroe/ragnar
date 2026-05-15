;;;
;;; Sum all mutiples of 3 or 5 below 1000
;;;     to test the Ragnar language.
;;;

; Solution 1: Function and while loop

f: func [n limit] [
     sum: 0
     i:   0
     while [ i < limit ] [
       i:   add i n
       sum: add sum i
     ]
   ]

limit:  1000
answer: (f 3 limit) + (f 5 limit) - (f 15 limit)
print ["Solution 1: The answer is" answer]


; Solution 2: Recursive list eater

zero?:    func [x] [ equal? 0 x ]
include?: func [x] [ or zero? x // 3
	                zero? x // 5 ]

f: func [limit] [
     inner: func [acc n] [
	      print [acc n]
	      either n > 0 [
	        if include? n [
	          acc: add acc n
		]
	        inner acc (n - 1)
	      ] [
                acc
	      ]
            ]
     inner 0 (limit - 1)
   ]

; THIS CURRENTLY BLOWS UP with a stack overflow. Implement tail call optimization!
print ["Solution 2: The answer is" f 1000]

