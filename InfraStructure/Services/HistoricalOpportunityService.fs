module HistoricalOpportunityService

open Models
open HistoricalOpportunityCalculation
open System.IO
open System
open FSharp.Data

let jsonToQuote (json: JsonValue): Quote =
    {
        Pair = json.["pair"].AsString()
        ExchangeId = decimal(json.["x"].AsInteger())
        BidPrice = json.["bp"].AsFloat() |> float
        AskPrice = json.["ap"].AsFloat() |> float
        BidSize = json.["bs"].AsFloat() |> float
        AskSize = json.["as"].AsFloat() |> float
        Timestamp = decimal (Int64.Parse(json.["t"].AsString().TrimEnd('L'))) // Parse as Int64
    }

// read input text file
let readAllText filePath =
    File.ReadAllText(filePath)

// parse JSON data
let textContent = readAllText "historicalData.txt"

// get historical data JSON content
let historicalJsonContent = JsonValue.Parse(textContent)

let convertJsonListToQuotes (jsonList: JsonValue): Quote list =
    match jsonList with
    | JsonValue.Array(items) -> 
        try items |> Array.toList |> List.map jsonToQuote
        with ex -> []
    | _ -> []

let quoteList = convertJsonListToQuotes historicalJsonContent
let startTime = DateTime(2023, 1, 26)
let endTime = DateTime(2023, 9, 27)

let getHistoricalOpportunities = findHistoricalOpportunities quoteList