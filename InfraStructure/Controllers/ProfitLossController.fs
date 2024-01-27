module ProfitLossController

open ProfitLossService
open Suave
open Suave.Successful
open System
open Suave.RequestErrors


let findValueByKey (formData: (string * string) list) (key: string) = 
    let tryFinding = formData 
                    |> List.tryFind (fun (k, _) -> k = key) 
                    |> Option.map snd
    match tryFinding with
    | Some value -> value
    | None -> "0"


let tryCreateDateTime year month day : Result<DateTime, string> =
    try
        Ok(DateTime(year, month, day))
    with
    | :? System.ArgumentException ->
        Error "Invalid date parameters"


let retrieveProfit (ctx : HttpContext) : Async<HttpContext option> =
    let formData =  ctx.request.multiPartFields
    let startYear = int (findValueByKey formData "startYear")
    let startMonth = int (findValueByKey formData "startMonth")
    let startDay = int (findValueByKey formData "startDay")
    let endYear = int (findValueByKey formData "endYear")
    let endMonth = int (findValueByKey formData "endMonth")
    let endDay = int (findValueByKey formData "endDay")


    match tryCreateDateTime startYear startMonth startDay, tryCreateDateTime endYear endMonth endDay with
    | Ok startDate, Ok endDate ->
        let startDateDO = DateOnly.FromDateTime(startDate)
        let endDateDO = DateOnly.FromDateTime(endDate)
        let profit = queryProfit startDateDO endDateDO
        ctx |> OK (string profit)
    | Error errMsg, _ | _, Error errMsg ->
        ctx |> BAD_REQUEST errMsg


let retrieveAnnualizedReturn (ctx : HttpContext) : Async<HttpContext option> =
    let formData =  ctx.request.multiPartFields
    let startYear = int (findValueByKey formData "startYear")
    let startMonth = int (findValueByKey formData "startMonth")
    let startDay = int (findValueByKey formData "startDay")
    let endYear = int (findValueByKey formData "endYear")
    let endMonth = int (findValueByKey formData "endMonth")
    let endDay = int (findValueByKey formData "endDay")

    match tryCreateDateTime startYear startMonth startDay, tryCreateDateTime endYear endMonth endDay with
    | Ok startDate, Ok endDate ->
        let startDateDO = DateOnly.FromDateTime(startDate)
        let endDateDO = DateOnly.FromDateTime(endDate)
        let annualizedReturn = calculateAnnualizedReturn startDateDO endDateDO
        ctx |> OK (string annualizedReturn)
    | Error errMsg, _ | _, Error errMsg ->
        ctx |> BAD_REQUEST errMsg
