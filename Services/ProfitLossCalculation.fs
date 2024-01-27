module ProfitLossCalculation

open Models
open System

let calculateInflow (t: CompletedTransaction) = t.BuyAmount * t.BuyPrice

let calculateOutflow (t: CompletedTransaction) = t.SellAmount * t.SellPrice

let calculateProfit (transaction: CompletedTransaction) =
    let purchaseCost = calculateInflow transaction
    let saleRevenue = calculateOutflow transaction
    saleRevenue - purchaseCost

let mockStopTrading = 
    printfn "Mock: trading stopped."

// settings should be saved in database in the next milestone
let mutable profitThreshold = 0M
let mutable alertEnabled = false
let mutable autoStopEnabled = false

let updateProfitThresholdSettings newThreshold newAlertEnabled newAutoStopEnabled =
    profitThreshold <- newThreshold
    alertEnabled <- newAlertEnabled
    autoStopEnabled <- newAutoStopEnabled

let fractionOfYear (startDate: DateOnly) (endDate: DateOnly) =
    let dateTime1 = DateTime(startDate.Year, startDate.Month, startDate.Day)
    let dateTime2 = DateTime(endDate.Year, endDate.Month, endDate.Day)
    let difference = dateTime2 - dateTime1
    difference.TotalDays / 365.0


