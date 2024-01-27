module StrategyController

open Models
open StrategyDao
open QueueSender
open Suave
open Suave.Successful
open Suave.RequestErrors

let findValueByKey (formData: (string * string) list) (key: string) = 
    let tryFinding = formData 
                    |> List.tryFind (fun (k, _) -> k = key) 
                    |> Option.map snd
    match tryFinding with
    | Some value -> value
    | None -> "0"

let checkStrategyFields strategy =
    (strategy.CurrencyNumber, strategy.MinSpread, strategy.MinProfit, strategy.MaxTransaction, strategy.MaxDailyTransaction)


let inputStrategy (ctx : HttpContext) : Async<HttpContext option> =

    let formData =  ctx.request.multiPartFields
    let strategy = {
        CurrencyNumber = int (findValueByKey formData "currencyNumber")
        MinSpread = float (findValueByKey formData "minSpread")
        MinProfit = float (findValueByKey formData "minProfit")
        MaxTransaction = float (findValueByKey formData "maxTransaction")
        MaxDailyTransaction = float (findValueByKey formData "maxDailyTransaction")
    }
    match checkStrategyFields strategy with
    | (0, _, _, _, _) | (_, 0.0, _, _, _) | (_, _, 0.0, _, _) | (_, _, _, 0.0, _) | (_, _, _, _, 0.0) ->
        ctx |> BAD_REQUEST "request body error"
    | _ ->
        let response = upSertStrategy strategy

        sendMessageAsync StrategyChange |> Async.RunSynchronously
        
        ctx |> OK (strategy.ToString())


    
    
