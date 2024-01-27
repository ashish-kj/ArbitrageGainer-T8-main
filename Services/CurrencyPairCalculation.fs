module CurrencyPairCalculation

let getIntersect list1 list2 =
    let set1 = Set.ofList list1
    let set2 = Set.ofList list2
    Set.intersect set1 set2 |> Set.toList

let getCrossTradedList (list1: string list) (list2: string list) (list3: string list) = 
    let intersect1 = getIntersect list1 list2
    let intersect2 = getIntersect list1 list3
    let intersect3 = getIntersect list2 list3
    let combinedList = intersect1 @ intersect2 @ intersect3
    let intersectList = Set.ofList combinedList |> Set.toList
    intersectList
        |> List.filter (fun s -> s.EndsWith("USD"))
        |> List.map (fun s -> s.Substring(0, s.Length - 3) + "-USD")
   