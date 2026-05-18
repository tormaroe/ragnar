{

WORK IN PROGRESS

(defn make-adder [x]
  (let [y x]
    (fn [z] (+ y z))))
(def add2 (make-adder 2))
(add2 4)
-> 6

}

make-adder: func [x] [
    y: x
    func [z] [
        y: y + z
    ]
]

print {
    add2: make-adder 2
    print [ "add2(4) = " (add2 4) ]
    print [ "add2(5) = " (add2 5) ]
}