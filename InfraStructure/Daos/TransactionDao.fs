module TransactionDao

open Models
open System
open Azure
open Azure.Data.Tables

let storageConnString = "DefaultEndpointsProtocol=https;AccountName=arbitrage-gainer-storage;AccountKey=yLkIl5Kt7oFwc5XSzxPjn52HOLNBYnBhzilMX2z5uK8XvfRWAeTQPpApWBMhzTYAFP2uac2Nf9cTACDbgOCeZw==;TableEndpoint=https://arbitrage-gainer-storage.table.cosmos.azure.com:443/;"
let tableClient = TableServiceClient storageConnString

let table = tableClient.GetTableClient "transactions"

let createTransactionTable = table.CreateIfNotExists () |> ignore

type TransactionEntity (transaction: Transaction) =
    interface ITableEntity with
        member val ETag = ETag "" with get, set
        member val PartitionKey = "transaction" with get, set
        member val RowKey = string (transaction.OrderIds |> List.head) with get, set
        member val Timestamp = Nullable() with get, set

    new() = TransactionEntity(Unchecked.defaultof<Transaction>)
    member val OrderIds = transaction.OrderIds |> List.map string |> String.concat "," with get, set
    member val TransactionStatus = transaction.TransactionStatus.ToString() with get, set

let addTransaction (transaction: Transaction) =
    let entity = TransactionEntity(transaction)
    table.AddEntity entity

let getTransaction (orderId: int64) = 
    let query = $"PartitionKey eq 'transaction' and RowKey eq '{orderId}'"
    table.Query<TransactionEntity>(query)
    |> Seq.tryHead
    |> Option.map (fun entity -> 
        // Map back to Transaction type
        {
            OrderIds = entity.OrderIds |> String.split ','
            TransactionStatus = 
                match entity.TransactionStatus with
                | "FullyFulfilled" -> FullyFulfilled
                | "PartiallyFulfilled" -> PartiallyFulfilled
                | "OneSideFulfilled" -> OneSideFulfilled
                | _ -> failwith "Invalid TransactionStatus"
        }
    )
