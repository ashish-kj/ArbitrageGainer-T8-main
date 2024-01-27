module InflowDao

// open Models
open System
open Azure
open Azure.Data.Tables

let storageConnString = "DefaultEndpointsProtocol=https;AccountName=arbitrage-gainer-storage;AccountKey=yLkIl5Kt7oFwc5XSzxPjn52HOLNBYnBhzilMX2z5uK8XvfRWAeTQPpApWBMhzTYAFP2uac2Nf9cTACDbgOCeZw==;TableEndpoint=https://arbitrage-gainer-storage.table.cosmos.azure.com:443/;"
let tableClient = TableServiceClient storageConnString

let table = tableClient.GetTableClient "inflows"

let createStrategyTable = table.CreateIfNotExists () |> ignore

type InflowEntity (date: string, inflow: float) =
    interface ITableEntity with
        member val ETag = ETag "" with get, set
        member val PartitionKey = "" with get, set
        member val RowKey = "" with get, set
        member val Timestamp = Nullable() with get, set

    new() = InflowEntity("", 0)
    member val Date = date with get, set
    member val Inflow = inflow with get, set
    member val PartitionKey = "inflow-partition-key" with get, set
    member val RowKey = date with get, set


let existInflow (date: string)  = 
    let queryString = sprintf "PartitionKey eq 'inflow-partition-key' and RowKey eq '%s'" date
    let inflows = table.Query<InflowEntity> queryString
    inflows |> Seq.toList |> List.length > 0

let addInflow (date: string) (inflow: float) = 
    let newInflow = InflowEntity (date, inflow)
    table.AddEntity newInflow

let upSertInflow (date: string) (inflow: float) = 
    let inflowEntity = table.GetEntity<InflowEntity>("inflow-partition-key", date).Value
    inflowEntity.Date <- date
    inflowEntity.Inflow <- inflow
    table.UpsertEntity (inflowEntity, TableUpdateMode.Replace)

let getInflow (date: string) = 
    let inflowEntity = table.GetEntityIfExists<InflowEntity>("inflow-partition-key", date).Value
    inflowEntity.Inflow

// let testFunc (date: string) (inflow: float) = 
//     match existInflow date with
//     | false -> addInflow date inflow
//     | true -> upSertInflow date ((getInflow date) + inflow)

// let todayStr = "2023-11-30"
// let tomorrowStr = "2023-12-01"
// testFunc todayStr 240
