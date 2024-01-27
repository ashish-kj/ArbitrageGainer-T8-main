module CurrencyPairController

open Models
open CurrencyPairDao
open CurrencyPairService
open Suave
open Suave.Successful
open System.IO

let retrieveCrossTradedCurrencies (ctx : HttpContext) : Async<HttpContext option> =
    let response1 = addCurrencyPairs crossTradedList
    let response2 = getCurrencyPairs
    let filePath = "crossTradedCurrencies.txt"
    File.WriteAllLines(filePath, response2)
    ctx |> OK (response2.ToString())