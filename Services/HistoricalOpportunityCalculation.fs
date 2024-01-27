module HistoricalOpportunityCalculation

open System
open Models

// extract a list of distinct pairs that traded across all the three exchanges
let extractCommonTradedPairs (quotes: Quote list) =
    let filterPairsByExchangeId exchangeId =
        quotes
        |> List.choose (fun quote ->
            match quote.ExchangeId with
            | x when x = exchangeId -> Some quote.Pair
            | _ -> None)
        |> List.distinct
    let pairsExchangeId2 = filterPairsByExchangeId 2M
    let pairsExchangeId6 = filterPairsByExchangeId 6M
    let pairsExchangeId23 = filterPairsByExchangeId 23M
    let isInAllLists pair = 
        List.contains pair pairsExchangeId6 && List.contains pair pairsExchangeId23
    let commonPairs = pairsExchangeId2 |> List.filter isInAllLists
    List.distinct commonPairs

// let commonTradedPairs = extractCommonTradedPairs quoteList

// for each commonly traded pair, and a given time period
let dateToUnixTimestamp (date: DateTime) =
    let epoch = DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
    let unixTime = date.ToUniversalTime() - epoch
    unixTime.TotalMilliseconds

// filter the quotes of a specific currency pair and time period
let filterQuotesByCurrencyAndTime (quotes: Quote list) (currencyPair: string) (startTime: DateTime) (endTime: DateTime) =
    let startTimestamp = startTime |> dateToUnixTimestamp |> decimal 
    let endTimestamp = endTime |> dateToUnixTimestamp |> decimal

    quotes
    |> List.choose (fun quote ->
        match quote.Pair, quote.Timestamp with
        | pair, t when pair = currencyPair && t >= startTimestamp && t <= endTimestamp -> Some quote
        | _ -> None)


// let bitfinexQuotes = filterQuotesByCurrencyAndTime quoteList "MKR-USD" startTime endTime

// helper function: extract timestamp from data
let extractTimestamp (quote: Quote) =
    quote.Timestamp

// helper function：sorting quotes by timestamp
let sortQuotesByTimestamp (quotes: Quote list) =
    List.sortBy extractTimestamp quotes

// helper function: bucketizing quotes
let bucketizeQuotes (sortedQuotes: Quote list) =
    let addQuoteToBucket (acc, currentBucket, lastTimestamp) quote =
        let timestamp = extractTimestamp quote
        if timestamp - lastTimestamp <= 5M then
            acc, quote :: currentBucket, lastTimestamp // current bucket
        else
            (List.rev currentBucket :: acc), [quote], timestamp // new bucket
    let initialAcc = ([], [], 0M)
    List.fold addQuoteToBucket initialAcc sortedQuotes

// integrate sorting and bucketing
let bucketingFunc (quotes: Quote list) =
    let sortedQuotes = sortQuotesByTimestamp quotes
    let groupedQuotes, lastBucket, _ = bucketizeQuotes sortedQuotes
    List.rev (List.rev lastBucket :: groupedQuotes) // including the last bucket

// let bucketedQuotes = bucketingFunc bitfinexQuotes


let runMap (quotes: Quote list) (minSpread: float) =
    let rec findPairs (pairs: Quote list) =
        match pairs with
        | [] -> ("", 0)
        | h::tail ->
            let existsSpread = 
                tail |> List.exists (fun q -> h.ExchangeId <> q.ExchangeId && (h.BidPrice - q.AskPrice >= minSpread || q.BidPrice - h.AskPrice >= minSpread))
            if existsSpread then
                (h.Pair, 1)
            else
                findPairs tail
    findPairs quotes

// Mapper
let historicalCalculationMapper (bucketedQuotes: Quote list list) (minSpread: float) =
    bucketedQuotes
    |> List.map (fun quotes -> runMap quotes minSpread)

// Reducer
let historicalCalculationReducer (mapout: (string * int) list) =
    mapout
    |> List.groupBy fst
    |> List.map (fun (key, values) -> (key, values |> List.sumBy snd))

let findHistoricalOpportunities (quotes: Quote list) (startTime: DateTime) (endTime: DateTime) (minSpread: float) = 
    let currencyPairs = extractCommonTradedPairs quotes
    let filteredResult = currencyPairs |> List.map (fun currencyPair -> filterQuotesByCurrencyAndTime quotes currencyPair startTime endTime)
    let bucketedResult = filteredResult |> List.map bucketingFunc
    let mapResult = bucketedResult |> List.map (fun bucketedQuotes -> historicalCalculationMapper bucketedQuotes minSpread)
    let reduceResult = mapResult |> List.map historicalCalculationReducer
    let resultList = reduceResult |> List.concat
    resultList 
    |> List.filter (fun (fst, _) -> fst <> "")
    |> List.map (fun (currencyPair, opportunityNumber) -> 
    { CurrencyPair = currencyPair; OpportunityNumber = opportunityNumber })