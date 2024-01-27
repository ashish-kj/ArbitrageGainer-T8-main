module ProfitDao

// open Models
open System
open Azure
open Azure.Data.Tables

let storageConnString = "DefaultEndpointsProtocol=https;AccountName=arbitrage-gainer-storage;AccountKey=yLkIl5Kt7oFwc5XSzxPjn52HOLNBYnBhzilMX2z5uK8XvfRWAeTQPpApWBMhzTYAFP2uac2Nf9cTACDbgOCeZw==;TableEndpoint=https://arbitrage-gainer-storage.table.cosmos.azure.com:443/;"
let tableClient = TableServiceClient storageConnString

let table = tableClient.GetTableClient "profits"

let createStrategyTable = table.CreateIfNotExists () |> ignore

type ProfitEntity (date: string, profit: float) =
    interface ITableEntity with
        member val ETag = ETag "" with get, set
        member val PartitionKey = "" with get, set
        member val RowKey = "" with get, set
        member val Timestamp = Nullable() with get, set

    new() = ProfitEntity("", 0)
    member val Date = date with get, set
    member val Profit = profit with get, set
    member val PartitionKey = "profit-partition-key" with get, set
    member val RowKey = date with get, set


let existProfit (date: string)  = 
    let queryString = sprintf "PartitionKey eq 'profit-partition-key' and RowKey eq '%s'" date
    let profits = table.Query<ProfitEntity> queryString
    profits |> Seq.toList |> List.length > 0

let addProfit (date: string) (profit: float) = 
    let newProfit = ProfitEntity (date, profit)
    table.AddEntity newProfit

let upSertProfit (date: string) (profit: float) = 
    let profitEntity = table.GetEntity<ProfitEntity>("profit-partition-key", date).Value
    profitEntity.Date <- date
    profitEntity.Profit <- profit
    table.UpsertEntity (profitEntity, TableUpdateMode.Replace)

let getProfit (date: string) = 
    let profitEntity = table.GetEntityIfExists<ProfitEntity>("profit-partition-key", date).Value
    profitEntity.Profit

let getProfitList (startDate: string) (endDate: string) = 
    let queryString = sprintf "PartitionKey eq 'profit-partition-key' and RowKey ge '%s' and RowKey le '%s'" startDate endDate
    let profitEntities = table.Query<ProfitEntity> queryString
    profitEntities 
    |> Seq.map (fun entity -> (DateOnly.Parse(entity.Date), decimal entity.Profit))
    |> Seq.toList

// let todayStr = "2023-11-30"
// let tomorrowStr = "2023-12-01"

// let testFunc (date: string) (profit: float) = 
//     match existProfit date with
//     | false -> addProfit date profit
//     | true -> upSertProfit date ((getProfit date) + profit)

// getProfitList "2022-11-30" "2023-11-30"