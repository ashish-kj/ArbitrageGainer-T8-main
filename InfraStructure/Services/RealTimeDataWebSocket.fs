module RealTimeDataWebSocket

open System
open System.Net.WebSockets
open System.Threading
open System.Text
open Models
open HistoricalOpportunityService
open RealTimeTrading
open FSharp.Data
open OrderManagement
open Microsoft.FSharp.Control
open System.IO
open Newtonsoft.Json

let apiKey = "vjQ43G04_B2e1ygcVjYvIXd6R28D_x9a"
let filePath = "identifiedArbitrageOpportunities.txt"

let saveOpportunityToFile buyOrder sellOrder =
    // The information needed is: crypto currency exchange ID, currency pair, bid and ask prices and amounts. (got from buyOrder & sellOrder)
    let buyOrderJson = 
        JsonConvert.SerializeObject(
            dict [
                "ExchangeId", box buyOrder.ExchangeId
                "CurrencyPair", box buyOrder.Pair
                "Price", box buyOrder.Price
                "Amount", box buyOrder.Size
            ])

    let sellOrderJson = 
        JsonConvert.SerializeObject(
            dict [
                "ExchangeId", box sellOrder.ExchangeId
                "CurrencyPair", box sellOrder.Pair
                "Price", box sellOrder.Price
                "Amount", box sellOrder.Size
            ])

    let combinedJson = 
        sprintf "{ \"BuyOrder\": %s, \"SellOrder\": %s }\n" buyOrderJson sellOrderJson
    
    File.AppendAllText(filePath, combinedJson)

let emitOrderFromQuote (quote: Quote) :unit =
    let response = realTimeTradingMailbox.PostAndReply(fun replyChannel -> ReceiveQuote(quote, replyChannel))
    match response with
    | Some(buyOrder: Order, sellOrder: Order) ->
        // 12.13 update: log the identified arbitrage opportunities (from real time market data) in a text file. INSTEAD OF use OrderManagement.emitOrderToExchange()
        saveOpportunityToFile buyOrder sellOrder

        // emitOrderToExchange buyOrder
        // emitOrderToExchange sellOrder
    | None -> ()

let connectAndSubscribe (pairs: string list) =
    async {
        printfn "RealTimeDataWebSocket.fs connectAndSubscribe() running..."
        use clientWebSocket = new ClientWebSocket()
        let cancellationToken = CancellationToken.None
        // let uri = Uri("wss://socket.polygon.io/crypto")
        let uri = Uri("wss://one8656-live-data.onrender.com")

        do! clientWebSocket.ConnectAsync(uri, cancellationToken) |> Async.AwaitTask

        // Authentication
        let authMessage = $"{{\"action\":\"auth\",\"params\":\"{apiKey}\"}}"
        printfn "RealTimeDataWebSocket: Sending auth message: %s" authMessage
        let authBytes = Encoding.UTF8.GetBytes(authMessage)
        let authSegment = ArraySegment<byte>(authBytes)
        do! clientWebSocket.SendAsync(authSegment, WebSocketMessageType.Text, true, cancellationToken) |> Async.AwaitTask

        let pairsParam =
            match pairs with
            // Subscribe to all crypto pairs
            | [] -> "XQ.*"
            // Subscribe to specified pairs
            | ps -> String.Join(",", ps |> List.map (fun p -> $"XQ.{p}"))
        // TODO: we should only subscribe to exchanges Bitfinex, Kraken, and Bitstamp. But I don't know their exchange ids.
        let subscribeMessage = $"{{\"action\":\"subscribe\", \"params\":\"{pairsParam}\"}}"
        printfn "RealTimeDataWebSocket: Sending subscribe message: %s" subscribeMessage
        let messageBytes = Encoding.UTF8.GetBytes(subscribeMessage)
        let messageSegment = ArraySegment<byte>(messageBytes)
        do! clientWebSocket.SendAsync(messageSegment, WebSocketMessageType.Text, true, cancellationToken) |> Async.AwaitTask        
        
        let rec receiveMessage () =
            async {
                let buffer = ArraySegment<byte>(Array.zeroCreate 8192)
                let! result = clientWebSocket.ReceiveAsync(buffer, cancellationToken) |> Async.AwaitTask
                printfn "RealTimeDataWebSocket: Received message type: %A" result.MessageType
                match result.MessageType <> WebSocketMessageType.Close with
                | true ->
                    let message = Encoding.UTF8.GetString(buffer.Array, buffer.Offset, result.Count)
                    printfn "RealTimeDataWebSocket: Received message: %s" message
                    message
                    |> JsonValue.Parse
                    |> convertJsonListToQuotes
                    // send to RealTimeTrading to process
                    |> List.iter emitOrderFromQuote
                    return! receiveMessage ()
                | _ -> return ()
            }

        return! receiveMessage ()

    } |> Async.Start
