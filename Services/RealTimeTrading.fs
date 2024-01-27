module RealTimeTrading

open System
open System.Collections.Generic
open Models

type RealTimeTradingMessage =
    | StartTrading
    | StopTrading
    | ReceiveQuote of Quote * AsyncReplyChannel<Option<Order * Order>>
    | UpdateTradingStrategy of Strategy
    | SetTopPairs of string list

// RecentQuotes should only include those following quotes to be compared:
// for each tracked currency pair:
// 1. The quote with lowest ask price
// 2. The quote with highest bid price
type RecentQuotes = {
    Pair: string
    Quotes : Quote list
}

let spreadValue (quoteToBuy: Quote) (quoteToSell: Quote) =
    quoteToSell.BidPrice - quoteToBuy.AskPrice

let tradableSize (quoteToBuy: Quote) (quoteToSell: Quote) =
    min quoteToSell.BidSize quoteToBuy.AskSize

let checkMinSpread (quoteToBuy: Quote) (quoteToSell: Quote) (strategy: Strategy) =
    spreadValue quoteToBuy quoteToSell >= strategy.MinSpread

let checkMinProfit (quoteToBuy: Quote) (quoteToSell: Quote) (strategy: Strategy) =
    let size: float = tradableSize quoteToBuy quoteToSell
    let profit = size * spreadValue quoteToBuy quoteToSell
    profit >= strategy.MinProfit

let orderValue (quoteToBuy: Quote) (quoteToSell: Quote) =
    let size: float = tradableSize quoteToBuy quoteToSell
    let buyOrderValue: float = size * quoteToBuy.AskPrice
    let sellOrderValue = size * quoteToSell.BidPrice
    buyOrderValue + sellOrderValue

let checkMaxTransactionAmount (quoteToBuy: Quote) (quoteToSell: Quote) (transactionAmount: float) (strategy: Strategy) =
    orderValue quoteToBuy quoteToSell + transactionAmount <= strategy.MaxTransaction

let checkDailyTransactionVolume (quoteToBuy: Quote) (quoteToSell: Quote) (dailyTransactionAmount: float) (strategy: Strategy) =
    orderValue quoteToBuy quoteToSell + dailyTransactionAmount <= strategy.MaxTransaction

let checkTradingStrategy (quoteToBuy: Quote) (quoteToSell: Quote) (transactionAmount: float, dailyTransactionAmount: float) (strategy: Strategy) =
    checkMinSpread quoteToBuy quoteToSell strategy &&
    checkMinProfit quoteToBuy quoteToSell strategy &&
    checkMaxTransactionAmount quoteToBuy quoteToSell transactionAmount strategy &&
    checkDailyTransactionVolume quoteToBuy quoteToSell transactionAmount strategy

let timestampToDateTime (timestamp: decimal) =
    let baseDate = DateTime(1970, 1, 1)
    // Assuming the timestamp is in milliseconds, adjust if it's in a different unit
    let milliseconds = Decimal.ToInt64(timestamp)
    baseDate.AddMilliseconds(float timestamp)

let isSameDay (timestamp1: decimal) (timestamp2: decimal) =
    let dateTime1 = timestampToDateTime timestamp1
    let dateTime2 = timestampToDateTime timestamp2
    dateTime1.Date = dateTime2.Date

let getCurrentTimestamp() =
    let now = DateTime.UtcNow
    let epoch = DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)
    let timestamp = now - epoch
    decimal timestamp.TotalMilliseconds

// create order from quoteToBuy and quoteToSell
let createOrder (quoteToBuy: Quote, quoteToSell: Quote) : Order * Order =
    let size: float = tradableSize quoteToBuy quoteToSell
    let id1 = Guid.NewGuid().ToString()
    let id2 = Guid.NewGuid().ToString()
    let buyOrder = { ExchangeId = quoteToBuy.ExchangeId; Pair = quoteToBuy.Pair; Size = size; OrderType = Buy; OrderStatus = Created; OrderId = id1; RelatedOrderId = id2; RealizedSize = 0.0; Price = quoteToBuy.AskPrice}
    let sellOrder = { ExchangeId = quoteToSell.ExchangeId; Pair = quoteToSell.Pair; Size = size; OrderType = Sell; OrderStatus = Created; OrderId = id2; RelatedOrderId = id1; RealizedSize = 0.0; Price = quoteToSell.BidPrice}
    (buyOrder, sellOrder)

// update RecentQuotes with current pair
// keep only quoteWithHighestBidPrice and quoteWithLowestAskPrice for each pair
// quoteToBuy and quoteToSell should not be included anymore
let updateRecentQuotes (quote: Quote) (recentQuotes: RecentQuotes list) (pairs: string list) (quoteToBuyAndSell: Option<Quote * Quote>) : RecentQuotes list =
    let updatedQuotes = recentQuotes |> List.filter (fun (r: RecentQuotes) -> r.Pair <> quote.Pair)
    let thisPairQuotes = recentQuotes |> List.filter (fun (r: RecentQuotes) -> r.Pair = quote.Pair)
    match thisPairQuotes with
        | [] -> { Pair = quote.Pair; Quotes = [ quote ] } :: updatedQuotes
        | thisPairQuote :: _ ->
            let candidates_ =
                quote :: thisPairQuote.Quotes
                // not equals to quoteToBuy or quoteToSell
                |> List.filter (fun q -> 
                    match quoteToBuyAndSell with
                    | Some(qToBuy, qToSell) -> quote <> qToBuy && quote <> qToSell
                    | None -> true // If both are None, there's nothing to compare with
                )
            match candidates_ with
            | [] -> updatedQuotes
            | candidates -> 
                let quoteWithHighestBidPrice = candidates |> List.maxBy (fun q -> q.BidPrice)
                let quoteWithLowestAskPrice = candidates |> List.minBy (fun q -> q.AskPrice)
                let newRecentQuotes =
                    match quoteWithHighestBidPrice = quoteWithLowestAskPrice with
                    | true -> { Pair = quote.Pair; Quotes = [ quote ] }
                    | false -> { Pair = quote.Pair; Quotes = [ quoteWithHighestBidPrice; quoteWithLowestAskPrice ] }
                newRecentQuotes :: updatedQuotes

// process real-time quote with trading strategy, compare with recent quotes, generate orders, maintain recentQuotes cachetransactionAmount, numberOfTodayTransactions.
let processRealTimeQuote (quote: Quote) (recentQuotes: RecentQuotes list)
                         (strategy: Strategy) (transactionAmount: float) 
                         (dailyTransactionAmount: float) (pairs: string list)
                         : (Option<Order * Order> * RecentQuotes list * float * float) =
    match List.contains quote.Pair pairs with
    | false -> (None, recentQuotes, transactionAmount, dailyTransactionAmount)
    | true ->
        let filteredQuotes = 
            recentQuotes
            |> List.choose (fun r -> if r.Pair = quote.Pair then Some r else None)
            |> List.truncate 1
        match filteredQuotes with
        | [] -> 
            let updatedQuotes = updateRecentQuotes quote recentQuotes pairs None
            (None, updatedQuotes, transactionAmount, dailyTransactionAmount)
        | recentQuote :: _ ->
            let possibleMatchedQuotes =
                filteredQuotes
                // match with every possible quote
                |> List.collect (fun rs -> 
                    rs.Quotes |> List.map (fun r -> (r, quote)))
                // quoteToBuy and quoteToSell can be swapped
                |> List.collect (fun (r1, r2) -> [(r1, r2); (r2, r1)])
                // check trading strategy
                |> List.choose (fun (quoteToBuy, quoteToSell) ->
                    match checkTradingStrategy quoteToBuy quoteToSell (transactionAmount, dailyTransactionAmount) strategy with
                    | true -> Some (quoteToBuy, quoteToSell)
                    | false -> None)
                |> List.truncate 1
            match possibleMatchedQuotes with
            | (quoteToBuy, quoteToSell) :: _ ->
                let (order1, order2) = createOrder (quoteToBuy, quoteToSell)
                let updatedQuotes = updateRecentQuotes quote recentQuotes pairs (Some (quoteToBuy, quoteToSell))
                let updatedTransactionAmount = transactionAmount + orderValue quoteToBuy quoteToSell
                let updatedNumberOfTodayTransactions = dailyTransactionAmount + orderValue quoteToBuy quoteToSell
                (Some (order1, order2), updatedQuotes, updatedTransactionAmount, updatedNumberOfTodayTransactions)
            | [] ->
                let updatedQuotes = updateRecentQuotes quote recentQuotes pairs None
                (None, updatedQuotes, transactionAmount, dailyTransactionAmount)

let realTimeTradingMailbox = 
    MailboxProcessor.Start(fun inbox ->
        let rec messageLoop (started: bool, strategy: Strategy, pairs: string list, recentQuotes: RecentQuotes list, transactionAmount: float, dailyTransactionAmount: float, lastQuoteTime: decimal) = async {
            let! message = inbox.Receive()
            match message with
            | StartTrading -> return! messageLoop (true, strategy, pairs, recentQuotes, transactionAmount, dailyTransactionAmount, lastQuoteTime)
            | StopTrading -> return! messageLoop (false, strategy, pairs, recentQuotes, transactionAmount, dailyTransactionAmount, lastQuoteTime)
            | ReceiveQuote (quote, replyChannel) ->
                match started with
                | false ->
                    replyChannel.Reply(None)
                    return! messageLoop (started, strategy, pairs, recentQuotes, transactionAmount, dailyTransactionAmount, lastQuoteTime)
                | true ->
                    printfn "RealTimeTrading: received quote: %A" quote
                    let (orders, updatedRecentQuotes, updatedAmount, updatedNumber) = processRealTimeQuote quote recentQuotes strategy transactionAmount dailyTransactionAmount pairs
                    match orders with
                    | Some (buyOrder: Order , sellOrder: Order) ->
                        replyChannel.Reply(Some (buyOrder, sellOrder))
                    | _ -> 
                        printfn "RealTimeTrading: opportunities not found."
                        replyChannel.Reply(None)

                    let updatedLastQuoteTime = quote.Timestamp
                    let isSameDay = isSameDay updatedLastQuoteTime lastQuoteTime
                    match isSameDay with
                    | true ->
                        return! messageLoop (started, strategy, pairs, updatedRecentQuotes, updatedAmount, updatedNumber, updatedLastQuoteTime)
                    | false ->
                        return! messageLoop (started, strategy, pairs, updatedRecentQuotes, updatedAmount, 0.0, updatedLastQuoteTime)
            | UpdateTradingStrategy newStrategy ->
                return! messageLoop (started, newStrategy, pairs, recentQuotes, transactionAmount, dailyTransactionAmount, lastQuoteTime)
            | SetTopPairs newPairs ->
                return! messageLoop (started, strategy, newPairs, recentQuotes, transactionAmount, dailyTransactionAmount, lastQuoteTime)
        }
        let defaultStrategy = {
            CurrencyNumber = 5
            MinSpread = 0.01
            MinProfit = 100.0
            MaxTransaction = 10000.0
            MaxDailyTransaction = 0
        }
        messageLoop (false, defaultStrategy, [], [], 0.0, 0, getCurrentTimestamp())
    )
