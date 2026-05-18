{

WORK IN PROGRESS

(defn make-adder [x]
  (let [y x]
    (fn [z] (+ y z))))
(def add2 (make-adder 2))
(add2 4)
-> 6

}

make-counter: func [start] [
    acc: start
    func [] [
        acc: acc + 1
    ]
]

_ print {
    counter: make-counter 0
    print [ "counter => " (counter) ]
    print [ "counter => " (counter) ]
}