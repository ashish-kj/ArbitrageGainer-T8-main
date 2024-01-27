module CurrencyPairService

open CurrencyPairCalculation
open System.IO

let bitfinexPath = "Bitfinex.txt"
let bitstampPath = "Bitstamp.txt"
let krakenPath = "Kraken.txt"

let readLinesToList path =
    File.ReadAllLines(path)
    |> Array.toList

let bitfinexList = readLinesToList bitfinexPath
let bitstampList = readLinesToList bitstampPath
let krakenList = readLinesToList krakenPath

let crossTradedList = getCrossTradedList bitfinexList bitstampList krakenList