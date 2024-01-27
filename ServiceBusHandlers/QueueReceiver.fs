module QueueReceiver

open Models
open StrategyDao
open OpportunityDao
open ProfitLossService
open RealTimeDataWebSocket
open RealTimeTrading
open Azure.Messaging.ServiceBus
open System

let handleStrategy (message : ServiceBusReceivedMessage) = 
    printfn "Service Bus receive message %s" (message.ApplicationProperties["Name"].ToString())
    let strategy = getStrategy ();
    realTimeTradingMailbox.Post(UpdateTradingStrategy strategy)

let handlePair (message : ServiceBusReceivedMessage) = 
    printfn "Service Bus receive message %s" (message.ApplicationProperties["Name"].ToString())
    let strategy = getStrategy ();
    // select top N pairs
    let topPairs = 
        getOpportunities strategy.CurrencyNumber
        |> List.map (fun opportunity -> opportunity.CurrencyPair)
    realTimeTradingMailbox.Post(SetTopPairs topPairs)
    connectAndSubscribe topPairs

let handleOrder (message : ServiceBusReceivedMessage) = 
    printfn "Service Bus receive message %s" (message.ApplicationProperties["Name"].ToString())
    let orderType = if message.ApplicationProperties.ContainsKey("OrderType") then (message.ApplicationProperties["OrderType"].ToString()) else ""
    let price = if message.ApplicationProperties.ContainsKey("Price") then float (message.ApplicationProperties["Price"].ToString()) else 0.0
    let size = if message.ApplicationProperties.ContainsKey("Size") then float (message.ApplicationProperties["Size"].ToString()) else 0.0
    let buyAmount = 
        match orderType with
        | "Buy" -> decimal size
        | _ -> 0M
    let sellAmount = 
        match orderType with
        | "Sell" -> decimal size
        | _ -> 0M
    let buyPrice = 
        match orderType with
        | "Buy" -> decimal price
        | _ -> 0M
    let sellPrice = 
        match orderType with
        | "Sell" -> decimal price
        | _ -> 0M
    let completedTransaction = { BuyAmount = buyAmount; BuyPrice = buyPrice; SellAmount = sellAmount; SellPrice = sellPrice }
    onCompleteTransaction (DateOnly.FromDateTime(DateTime.Now)) completedTransaction

let messageHandler(args : ProcessMessageEventArgs) = 
    let message = args.Message
    task {
        if message.ApplicationProperties.ContainsKey("Name") then
            let messageName = message.ApplicationProperties["Name"].ToString()
            match messageName with
            | "Strategy" -> handleStrategy message     
            | "Pair" -> handlePair message
            | "Order" -> handleOrder message 
            | _ -> () 
    }

let ErrorHandler (args: ProcessErrorEventArgs) =
    task {
        // the error source tells me at what point in the processing an error occurred
        Console.WriteLine(args.ErrorSource);
        // the fully qualified namespace is available
        Console.WriteLine(args.FullyQualifiedNamespace);
        // as well as the entity path
        Console.WriteLine(args.EntityPath);
        Console.WriteLine(args.Exception.ToString());
    }

let startReceiveAsync (queueName : string) = async {

    let client = ServiceBusClient("Endpoint=sb://arbitrage-gainer-servicebus.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=jxnoEqHx0sJ0Bsi+C/yGFFIANYYQWRniE+ASbJFobaA=")

    let processor = client.CreateProcessor(queueName)

    processor.add_ProcessMessageAsync(fun args -> messageHandler args)

    processor.add_ProcessErrorAsync(fun args -> ErrorHandler args)

    processor.StartProcessingAsync() |> ignore
    
}