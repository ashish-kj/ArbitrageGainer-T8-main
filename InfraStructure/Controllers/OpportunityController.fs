module OpportunityController

open Models
open OpportunityDao
open HistoricalOpportunityService
open StrategyDao
open QueueSender
open System
open Suave
open Suave.Successful
open Newtonsoft.Json
open System.IO
open Suave.RequestErrors

let findValueByKey (formData: (string * string) list) (key: string) = 
    let tryFinding = formData 
                    |> List.tryFind (fun (k, _) -> k = key) 
                    |> Option.map snd
    match tryFinding with
    | Some value -> value
    | None -> "0"

let retrieveHistoricalOpportunities (ctx : HttpContext) : Async<HttpContext option> =
    let formData =  ctx.request.multiPartFields
    let startYear = int (findValueByKey formData "startYear")
    let startMonth = int (findValueByKey formData "startMonth")
    let startDay = int (findValueByKey formData "startDay")
    let endYear = int (findValueByKey formData "endYear")
    let endMonth = int (findValueByKey formData "endMonth")
    let endDay = int (findValueByKey formData "endDay")
    let strategy = getStrategy ()
    let minSpread = strategy.MinSpread
    let startTime = DateTime(startYear, startMonth, startDay)
    let endTime = DateTime(endYear, endMonth, endDay)
    let historicalOpportunities = getHistoricalOpportunities startTime endTime minSpread
    let opportunitiesJson = JsonConvert.SerializeObject(historicalOpportunities, Formatting.Indented)
    let filePath = "historicalAribtrageOpportunitites.txt"
    File.WriteAllText(filePath, opportunitiesJson)
    let response = addOpportunities historicalOpportunities

    sendMessageAsync TopPairChange |> Async.RunSynchronously

    ctx |> OK (historicalOpportunities.ToString())