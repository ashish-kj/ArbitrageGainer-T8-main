module StrategyDao

open Models
open System
open Azure
open Azure.Data.Tables
open System.Linq



let storageConnString = "DefaultEndpointsProtocol=https;AccountName=arbitrage-gainer-storage;AccountKey=yLkIl5Kt7oFwc5XSzxPjn52HOLNBYnBhzilMX2z5uK8XvfRWAeTQPpApWBMhzTYAFP2uac2Nf9cTACDbgOCeZw==;TableEndpoint=https://arbitrage-gainer-storage.table.cosmos.azure.com:443/;"
let tableClient = TableServiceClient storageConnString

let table = tableClient.GetTableClient "strategies"

let createStrategyTable = table.CreateIfNotExists () |> ignore

// createStrategyTable

type StrategyEntity (currencyNumber: int, minSpread: float, minProfit: float, maxTransaction: float, maxDailyTransaction: float) =
    interface ITableEntity with
        member val ETag = ETag "" with get, set
        member val PartitionKey = "" with get, set
        member val RowKey = "" with get, set
        member val Timestamp = Nullable() with get, set

    new() = StrategyEntity(0, 0.0, 0.0, 0.0, 0.0)
    member val CurrencyNumber = currencyNumber with get, set
    member val MinSpread = minSpread with get, set
    member val MinProfit = minProfit with get, set
    member val MaxTransaction = maxTransaction with get, set
    member val MaxDailyTransaction = maxDailyTransaction with get, set
    member val PartitionKey = "strategy-partition-key" with get, set
    member val RowKey = "strategy-row-key" with get, set

let addStrategy(strategy: Strategy) = 
    let newStrategy = StrategyEntity (strategy.CurrencyNumber, strategy.MinSpread, strategy.MinProfit, strategy.MaxTransaction, strategy.MaxDailyTransaction)
    table.AddEntity newStrategy
    
let existStrategy = 
    let strategies = table.Query<StrategyEntity> "PartitionKey eq 'strategy-partition-key'"
    strategies |> Seq.toList |> List.length > 0

let upSertStrategy(strategy: Strategy) = 
    let strategyEntity = table.GetEntity<StrategyEntity>("strategy-partition-key", "strategy-row-key").Value
    strategyEntity.CurrencyNumber <- strategy.CurrencyNumber
    strategyEntity.MinSpread <- strategy.MinSpread
    strategyEntity.MinProfit <- strategy.MinProfit
    strategyEntity.MaxTransaction <- strategy.MaxTransaction
    strategyEntity.MaxDailyTransaction <- strategy.MaxDailyTransaction
    table.UpsertEntity (strategyEntity, TableUpdateMode.Replace)

let getStrategy () = 
    let strategyEntity = table.GetEntityIfExists<StrategyEntity>("strategy-partition-key", "strategy-row-key").Value
    let strategy = {
        CurrencyNumber = strategyEntity.CurrencyNumber
        MinSpread = strategyEntity.MinSpread
        MinProfit = strategyEntity.MinProfit
        MaxTransaction = strategyEntity.MaxTransaction
        MaxDailyTransaction = strategyEntity.MaxDailyTransaction
    }
    strategy
