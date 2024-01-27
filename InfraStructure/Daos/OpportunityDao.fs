module OpportunityDao

open Models
open System
open Azure
open Azure.Data.Tables

let storageConnString = "DefaultEndpointsProtocol=https;AccountName=arbitrage-gainer-storage;AccountKey=yLkIl5Kt7oFwc5XSzxPjn52HOLNBYnBhzilMX2z5uK8XvfRWAeTQPpApWBMhzTYAFP2uac2Nf9cTACDbgOCeZw==;TableEndpoint=https://arbitrage-gainer-storage.table.cosmos.azure.com:443/;"
let tableClient = TableServiceClient storageConnString

let table = tableClient.GetTableClient "opportunities"

let createOpportunityTable = table.CreateIfNotExists () |> ignore

type OpportunityEntity (currencyPair: string, opportunityNumber: int) =
    interface ITableEntity with
        member val ETag = ETag "" with get, set
        member val PartitionKey = "" with get, set
        member val RowKey = "" with get, set
        member val Timestamp = Nullable() with get, set

    new() = OpportunityEntity("", 0)
    member val currencyPair = currencyPair with get, set
    member val opportunityNumber = opportunityNumber with get, set
    member val PartitionKey = "opportunity-partition-key" with get, set
    member val RowKey = string currencyPair with get, set

let addOpportunities (opportunities: Opportunity list) =
    let opportunityEntities = opportunities |> List.map (fun opportunity -> OpportunityEntity(opportunity.CurrencyPair, opportunity.OpportunityNumber))
    opportunityEntities
    |> List.map (fun opportunity -> TableTransactionAction (TableTransactionActionType.UpsertReplace, opportunity))
    |> table.SubmitTransaction

// Get top N opportunities
let getOpportunities number = 
    let opportunityEntities = table.Query<OpportunityEntity> "PartitionKey eq 'opportunity-partition-key'"
    let sortedSeq = opportunityEntities 
                    |> Seq.map (fun entity -> { CurrencyPair = entity.currencyPair; OpportunityNumber = entity.opportunityNumber})
                    |> Seq.sortByDescending (fun opportunity -> opportunity.OpportunityNumber)
    let topNumber = 
        match number > Seq.length sortedSeq with
        | true -> Seq.length sortedSeq
        | false -> number
    sortedSeq                           
    |> Seq.take topNumber
    |> Seq.toList
