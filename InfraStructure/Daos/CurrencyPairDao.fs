module CurrencyPairDao

open Models
open System
open Azure
open Azure.Data.Tables
open System.Linq

let storageConnString = "DefaultEndpointsProtocol=https;AccountName=arbitrage-gainer-storage;AccountKey=yLkIl5Kt7oFwc5XSzxPjn52HOLNBYnBhzilMX2z5uK8XvfRWAeTQPpApWBMhzTYAFP2uac2Nf9cTACDbgOCeZw==;TableEndpoint=https://arbitrage-gainer-storage.table.cosmos.azure.com:443/;"
let tableClient = TableServiceClient storageConnString

let table = tableClient.GetTableClient "currency_pairs"

let createCurrencyPairTable = table.CreateIfNotExists () |> ignore

type CurrencyPairEntity (currencyPair: string) =
    interface ITableEntity with
        member val ETag = ETag "" with get, set
        member val PartitionKey = "" with get, set
        member val RowKey = "" with get, set
        member val Timestamp = Nullable() with get, set

    new() = CurrencyPairEntity("")
    member val currencyPair = currencyPair with get, set
    member val PartitionKey = "currency-pair-partition-key" with get, set
    member val RowKey = string currencyPair with get, set

let addCurrencyPairs (currencyPairs: string list) =
    let currencyEntities = currencyPairs |> List.map (fun currencyPair -> CurrencyPairEntity(currencyPair))
    currencyEntities
    |> List.map (fun currencyPair -> TableTransactionAction (TableTransactionActionType.UpsertReplace, currencyPair))
    |> table.SubmitTransaction

// let currencyPairs: string list =   ["1INCH-USD"; "AAVE-USD"; "ADA-USD"; "ALGO-USD"; "ALPHA-USD"; "AMP-USD"; "ANT-USD"]
// addCurrencyPairs currencyPairs

let getCurrencyPairs = 
    let currencyPairEntities = table.Query<CurrencyPairEntity> "PartitionKey eq 'currency-pair-partition-key'"
    currencyPairEntities 
    |> Seq.map (fun entity -> entity.currencyPair)
    |> Seq.toList