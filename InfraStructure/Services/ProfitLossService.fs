module ProfitLossService

open Models
open ProfitLossCalculation
open ProfitDao
open InflowDao
open OutflowDao
open System
open EmailService

// update profit in database
let onCompleteTransaction (date: DateOnly) (transaction: CompletedTransaction) =
    let profitChange = calculateProfit transaction
    let inflowChange = calculateInflow transaction
    let outflowChange = calculateOutflow transaction
    let dateStr = date.ToString().Replace('/', '-')

    let profitRes = match existProfit dateStr with
                    | false -> addProfit dateStr (float profitChange)
                    | true -> upSertProfit dateStr ((getProfit dateStr) + float profitChange)

    let inflowRes = match existInflow dateStr with
                    | false -> addInflow dateStr (float inflowChange)
                    | true -> upSertInflow dateStr ((getInflow dateStr) + float inflowChange)

    let outflowRes = match existOutflow dateStr with
                        | false -> addOutflow dateStr (float outflowChange)
                        | true -> upSertOutflow dateStr ((getOutflow dateStr) + float outflowChange)
    ()


// query profit in database based on time span
let queryProfit (startDate: DateOnly) (endDate: DateOnly) =
    let startDateStr = startDate.ToString().Replace('/', '-')
    let endDateStr = endDate.ToString().Replace('/', '-')
    getProfitList startDateStr endDateStr

// query today's profit
let queryTodayProfit =
    let today = DateOnly.FromDateTime(DateTime.Now)
    let todayProfitList = queryProfit today today
    match todayProfitList.Length with
    | 0 -> 0M
    | _ -> todayProfitList
        |> List.head
        |> snd


let checkProfitAndTriggerActions =
    let todayProfit = queryTodayProfit
    match alertEnabled, todayProfit >= profitThreshold with
        | true, true ->
            sendEmail ("fppt8test@gmail.com", "Trading Profit & Loss Service Notification", "Profit threshold reached.")
            match autoStopEnabled with
                | true -> mockStopTrading
                | _ -> ()
        | _ -> ()


let queryTotalOutflowSum (startDate: DateOnly) (endDate: DateOnly) =
    let startDateStr = startDate.ToString().Replace('/', '-')
    let endDateStr = endDate.ToString().Replace('/', '-') 
    getOutflowList startDateStr endDateStr
    |> Seq.sumBy snd


let calculateAnnualizedReturn (startDate: DateOnly) (endDate: DateOnly) =
    let startDateStr = startDate.ToString().Replace('/', '-')

    let firstDayInflow =
        match existInflow startDateStr with
        | true -> decimal (getInflow startDateStr)
        | _ -> 0M

    let firstDayOutflow =
        match existOutflow startDateStr with
        | true -> decimal (getOutflow startDateStr)
        | _ -> 0M

    let totalOutflow = double(queryTotalOutflowSum startDate endDate)
    let initialInvestment = ((double)firstDayInflow) - ((double)firstDayOutflow)
    let fractionOfYear = fractionOfYear startDate endDate
    printfn "totalOutflow: %f, initialInvestment: %f, fractionOfYear: %f" totalOutflow initialInvestment fractionOfYear
    match initialInvestment > 0.0 with
    | true -> Math.Pow(totalOutflow / initialInvestment, 1.0 / fractionOfYear) - 1.0
    | false -> 0.0
    


// let completedTransaction1 = 
//     { BuyAmount = 3.0M; BuyPrice = 50.0M; SellAmount = 3.0M; SellPrice = 55.0M }

// let completedTransaction2 = 
//     { BuyAmount = 2.0M; BuyPrice = 20.0M; SellAmount = 2.0M; SellPrice = 30.0M }

// updateProfitThresholdSettings 10M true true
// onCompleteTransaction (DateOnly.FromDateTime(DateTime.Now)) completedTransaction1
// onCompleteTransaction (DateOnly.FromDateTime(DateTime.Now)) completedTransaction2
// checkProfitAndTriggerActions

// // set Initial Investment for test (make sure it is positive)
// onCompleteTransaction (DateOnly.FromDateTime(DateTime.Now.AddDays(-365.0))) { BuyAmount = 1.0M; BuyPrice = 10000.0M; SellAmount = 1.0M; SellPrice = 5000.0M }

// calculateAnnualizedReturn (DateOnly.FromDateTime(DateTime.Now.AddDays(-365.0))) (DateOnly.FromDateTime(DateTime.Now))