module OutflowDao

// open Models
open System
open Azure
open Azure.Data.Tables

let storageConnString = "DefaultEndpointsProtocol=https;AccountName=arbitrage-gainer-storage;AccountKey=yLkIl5Kt7oFwc5XSzxPjn52HOLNBYnBhzilMX2z5uK8XvfRWAeTQPpApWBMhzTYAFP2uac2Nf9cTACDbgOCeZw==;TableEndpoint=https://arbitrage-gainer-storage.table.cosmos.azure.com:443/;"
let tableClient = TableServiceClient storageConnString

let table = tableClient.GetTableClient "outflows"

let createStrategyTable = table.CreateIfNotExists () |> ignore

type OutflowEntity (date: string, outflow: float) =
    interface ITableEntity with
        member val ETag = ETag "" with get, set
        member val PartitionKey = "" with get, set
        member val RowKey = "" with get, set
        member val Timestamp = Nullable() with get, set

    new() = OutflowEntity("", 0)
    member val Date = date with get, set
    member val Outflow = outflow with get, set
    member val PartitionKey = "outflow-partition-key" with get, set
    member val RowKey = date with get, set


let existOutflow (date: string)  = 
    let queryString = sprintf "PartitionKey eq 'outflow-partition-key' and RowKey eq '%s'" date
    let outflows = table.Query<OutflowEntity> queryString
    outflows |> Seq.toList |> List.length > 0

let addOutflow (date: string) (outflow: float) = 
    let newOutflow = OutflowEntity (date, outflow)
    table.AddEntity newOutflow

let upSertOutflow (date: string) (outflow: float) = 
    let outflowEntity = table.GetEntity<OutflowEntity>("outflow-partition-key", date).Value
    outflowEntity.Date <- date
    outflowEntity.Outflow <- outflow
    table.UpsertEntity (outflowEntity, TableUpdateMode.Replace)

let getOutflow (date: string) = 
    let outflowEntity = table.GetEntityIfExists<OutflowEntity>("outflow-partition-key", date).Value
    outflowEntity.Outflow

let getOutflowList (startDate: string) (endDate: string) = 
    let queryString = sprintf "PartitionKey eq 'outflow-partition-key' and RowKey ge '%s' and RowKey le '%s'" startDate endDate
    let outflowEntities = table.Query<OutflowEntity> queryString
    outflowEntities 
    |> Seq.map (fun entity -> (DateOnly.Parse(entity.Date), decimal entity.Outflow))
    |> Seq.toList

// let testFunc (date: string) (outflow: float) = 
//     match existOutflow date with
//     | false -> addOutflow date outflow
//     | true -> upSertOutflow date ((getOutflow date) + outflow)

// let todayStr = "2023-11-30"
// let tomorrowStr = "2023-12-01"

// testFunc todayStr 240

// getOutflowList "2022-11-30" "2023-11-30"