open StrategyController
open CurrencyPairController
open OpportunityController
open ProfitLossController
open TradingController
open QueueReceiver
open Suave
open Suave.Filters
open Suave.Operators
open Suave.Successful

let cfg = { defaultConfig with
                bindings = [ HttpBinding.createSimple HTTP "0.0.0.0" 8080  ] }

// Start listen to ServiceBus
startReceiveAsync("strategy-queue") |> Async.RunSynchronously
startReceiveAsync("order-queue") |> Async.RunSynchronously
startReceiveAsync("pair-queue") |> Async.RunSynchronously

let webApp = 
    choose 
        [   
            GET >=> choose [ 
                path "/" >=> OK "Hello, Suave!"
                path "/currencies" >=> retrieveCrossTradedCurrencies
                path "/profit" >=> retrieveProfit
                path "/annualized-return" >=> retrieveAnnualizedReturn
                path "/email" >=> sendEmail
            ]
            POST >=> choose [ 
                path "/strategy" >=> inputStrategy
                path "/opportunities" >=> retrieveHistoricalOpportunities

                path "/initiate-trading" >=> initiateTrading
                path "/stop-trading" >=> stopTrading
            ]
        ]

startWebServer cfg webApp