module TradingController

open Models
open StrategyDao
open Suave
open Suave.Successful
open Suave.RequestErrors
open OpportunityDao
open RealTimeDataWebSocket
open RealTimeTrading
open EmailService

let initiateTrading (ctx : HttpContext) : Async<HttpContext option> =
    printfn "TradingController.fs initiateTrading() running..."
    // get trading strategy from DB
    let strategy = getStrategy ();

    // select top N pairs
    let topPairs = 
        getOpportunities strategy.CurrencyNumber
        |> List.map (fun opportunity -> opportunity.CurrencyPair)

    // Start Trading
    realTimeTradingMailbox.Post(SetTopPairs topPairs)
    realTimeTradingMailbox.Post(UpdateTradingStrategy strategy)
    realTimeTradingMailbox.Post(StartTrading)

    // subscribe to real time data
    connectAndSubscribe topPairs

    ctx |> OK ("success")

let stopTrading (ctx : HttpContext) : Async<HttpContext option> =
    printfn "TradingController.fs stopTrading() running..."
    realTimeTradingMailbox.Post(StopTrading)
    ctx |> OK ("success")

let sendEmail (ctx : HttpContext) : Async<HttpContext option> =
    sendEmail ("fppt8test@gmail.com", "Question about your GPA", "Hi, Your GPA report is ready. Please check it out.")
    ctx |> OK ("success")
