module OrderManagement

open System
open System.Collections.Generic
open System.Net.Http
open System.Text
open Models
open OrderDao
open TransactionDao
open QueueSender
open EmailService

let submitOrderInBitfinex (order: Order) =
    async {
        use client = new HttpClient()

        client.DefaultRequestHeaders.Accept.Add(System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"))
        client.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/json") |> ignore
        client.DefaultRequestHeaders.Add("bfx-nonce", "xxx")
        client.DefaultRequestHeaders.Add("bfx-apikey", "xxx")
        client.DefaultRequestHeaders.Add("bfx-signature", "xxx")

        let jsonData = sprintf """ { "type": "EXCHANGE LIMIT", "symbol": "%s", "amount": "%f", "price": "%f" } """ order.Pair order.Size order.Price

        let content = new StringContent(jsonData, Encoding.UTF8, "application/json")

        let! response = client.PostAsync("https://18656-testing-server.azurewebsites.net/order/place/api/v2/:orderSide/market/:currencyPair/", content) |> Async.AwaitTask

        let! responseString = response.Content.ReadAsStringAsync() |> Async.AwaitTask
        ()
    }

let retriveOrderInBitfinex (order: Order) =
    async {
        use client = new HttpClient()

        client.DefaultRequestHeaders.Accept.Add(System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"))
        client.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/json") |> ignore
        client.DefaultRequestHeaders.Add("bfx-nonce", "xxx")
        client.DefaultRequestHeaders.Add("bfx-apikey", "xxx")
        client.DefaultRequestHeaders.Add("bfx-signature", "xxx")

        let url = sprintf "https://18656-testing-server.azurewebsites.net/order/status/api/v2/order_status/"

        let! response = client.PostAsync(url, null) |> Async.AwaitTask

        let! responseString = response.Content.ReadAsStringAsync() |> Async.AwaitTask
        return {order with OrderStatus = Fulfilled}
    }

let submitOrderInKraken (order: Order) =
    async {
        use client = new HttpClient()

        client.DefaultRequestHeaders.Accept.Add(System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"))
        client.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/json") |> ignore
        client.DefaultRequestHeaders.Add("bfx-nonce", "xxx")
        client.DefaultRequestHeaders.Add("bfx-apikey", "xxx")
        client.DefaultRequestHeaders.Add("bfx-signature", "xxx")

        let formData = new FormUrlEncodedContent([
            KeyValuePair<string, string>("nonce", "<YOUR-NONCE>")
            KeyValuePair<string, string>("pair", order.Pair)
            KeyValuePair<string, string>("type", match order.OrderType with | Buy -> "buy" | Sell -> "sell" | BuyRemaining -> "buy" | SellRemaining -> "sell" )
            KeyValuePair<string, string>("ordertype", "limit")
            KeyValuePair<string, string>("price", order.Price.ToString())
            KeyValuePair<string, string>("volume", order.Size.ToString())
            KeyValuePair<string, string>("leverage", "2:1")
        ])

        client.DefaultRequestHeaders.Add("API-Key", "xxx")
        client.DefaultRequestHeaders.Add("API-Sign", "xxx")
        client.DefaultRequestHeaders.Add("Content-Type", "application/x-www-form-urlencoded")

        let! response = client.PostAsync("https://18656-testing-server.azurewebsites.net/order/place/0/private/AddOrder", formData) |> Async.AwaitTask

        let! responseString = response.Content.ReadAsStringAsync() |> Async.AwaitTask
        ()
    }

let retriveOrderInKraken (order: Order) =
    async {
        use client = new HttpClient()
        let formData = new FormUrlEncodedContent([
            KeyValuePair<string, string>("nonce", order.OrderId.ToString())
            KeyValuePair<string, string>("trades", "true")
        ])

        client.DefaultRequestHeaders.Add("API-Key", "xxx")
        client.DefaultRequestHeaders.Add("API-Sign", "xxx")
        client.DefaultRequestHeaders.Add("Content-Type", "application/x-www-form-urlencoded")

        let! response = client.PostAsync("https://18656-testing-server.azurewebsites.net/order/status/0/private/QueryOrders", formData) |> Async.AwaitTask

        let! responseString = response.Content.ReadAsStringAsync() |> Async.AwaitTask
        return {order with OrderStatus = Fulfilled}
    }

let submitBuyOrderInBitstamp (order: Order) =
    async {
        use client = new HttpClient()

        client.DefaultRequestHeaders.Accept.Add(System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"))
        client.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/json") |> ignore

        let jsonData = sprintf """ { "amount": "%f", "client_order_id": "%s" } """ order.Size order.OrderId

        let content = new StringContent(jsonData, Encoding.UTF8, "application/json")

        let uri = sprintf "https://18656-testing-server.azurewebsites.net/order/place/v2/auth/w/order/submit"
        let! response = client.PostAsync(uri, content) |> Async.AwaitTask

        let! responseString = response.Content.ReadAsStringAsync() |> Async.AwaitTask
        ()
    }

let submitSellOrderInBitstamp (order: Order) =
    async {
        use client = new HttpClient()

        client.DefaultRequestHeaders.Accept.Add(System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"))
        client.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/json") |> ignore

        let jsonData = sprintf """ { "amount": "%f", "client_order_id": "%s" } """ order.Size order.OrderId

        let content = new StringContent(jsonData, Encoding.UTF8, "application/json")

        let uri = sprintf "https://18656-testing-server.azurewebsites.net/order/place/v2/auth/w/order/submit"
        let! response = client.PostAsync(uri, content) |> Async.AwaitTask

        let! responseString = response.Content.ReadAsStringAsync() |> Async.AwaitTask
        ()
    }

let retriveOrderInBitstamp (order: Order) =
    async {
        use client = new HttpClient()

        client.DefaultRequestHeaders.Accept.Add(System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"))
        client.DefaultRequestHeaders.TryAddWithoutValidation("Content-Type", "application/json") |> ignore

        let jsonData = sprintf """ { "id": "%s", "omit_transactions": true } """ order.OrderId

        let content = new StringContent(jsonData, Encoding.UTF8, "application/json")

        let! response = client.PostAsync("https://18656-testing-server.azurewebsites.net/order/status/auth/r/order/currencyPair:orderId/trades", content) |> Async.AwaitTask

        let! responseString = response.Content.ReadAsStringAsync() |> Async.AwaitTask
        return {order with OrderStatus = Fulfilled}
    }

let saveTransactionToDB (transaction: Transaction) =
    addTransaction transaction |> ignore
    printfn "Saving transaction to DB: %A" transaction

let rec submitOrderAndProcessResult (order, submitFunc, retrieveFunc) =
    async {
        do! submitFunc order
        do! Async.Sleep(5000)
        let! result = retrieveFunc order
        receiveOrderStatusFromExchange result
        ()
    }

and emitOrderToExchange (order: Order) =
    printfn "Emitting order to exchange: %A" order
    match (order.ExchangeId, order.OrderType) with
    // TODO: not sure about ids of Bitfinex, Kraken and Bitstamp.
    | (12300001M, _) -> submitOrderAndProcessResult (order, submitOrderInBitfinex, retriveOrderInBitfinex) |> ignore
    | (12300002M, _) -> submitOrderAndProcessResult (order, submitOrderInKraken, retriveOrderInKraken) |> ignore
    | (12300003M, a) when a = Buy || a = BuyRemaining -> submitOrderAndProcessResult (order, submitBuyOrderInBitstamp, retriveOrderInBitstamp) |> ignore
    | (12300003M, a) when a = Sell || a = SellRemaining -> submitOrderAndProcessResult (order, submitSellOrderInBitstamp, retriveOrderInBitstamp) |> ignore
    | _ -> ()
    addOrder (order) |> ignore
    ()

and receiveOrderStatusFromExchange (order: Order) =
    printfn "Receiving order status from exchange: %A" order
    // query all related order from db
    let orderOption = getOrder order.RelatedOrderId
    match orderOption with
    | None -> ()
    | Some order2 ->
        let orders = [order; order2]
        match List.exists (fun o -> o.OrderStatus = Created) orders with
        // wait for the last order to get updated
        | true -> ()
        | false ->
            match List.forall (fun o -> o.OrderStatus = Fulfilled) orders with
            | true -> 
                // For both order legs (buy and sell) fully fulfilled, store transactions history in a database.
                sendMessageAsync (CompletedOrder order) |> Async.RunSynchronously
                sendMessageAsync (CompletedOrder order2) |> Async.RunSynchronously
                let transaction: Transaction = { OrderIds = orders |> List.map (fun o -> o.OrderId); TransactionStatus = FullyFulfilled  }
                saveTransactionToDB transaction
            | false ->
                match List.exists (fun o -> o.OrderStatus = NotFilled) orders with
                | true -> 
                    // For orders that had only one side filled, notify the user via e-mail.
                    orders
                    |> List.filter (fun o -> o.OrderStatus = NotFilled)
                    |> List.iter (fun o -> 
                        sendEmail ("jinqianf@andrew.cmu.edu", "OrderManagement Notification", sprintf "Hi, Your order %s is only one side filled. Please check it out." order.OrderId)
                    )
                    let transaction: Transaction = { OrderIds = orders |> List.map (fun o -> o.OrderId); TransactionStatus = OneSideFulfilled  }
                    saveTransactionToDB transaction
                | false ->
                    // For partially fulfilled orders, emit one more order with the remaining amount (desired amount – booked amount) and store the realized transactions in a database when this new order gets updated.
                    orders
                    |> List.filter (fun o -> o.OrderStatus = Partial)
                    |> List.map (fun o -> { o with Size = o.Size - o.RealizedSize ; OrderId = Guid.NewGuid().ToString(); RelatedOrderId = o.OrderId ; OrderType = match o.OrderType with | Buy -> BuyRemaining | Sell -> SellRemaining | a -> a})
                    |> List.iter (fun o -> emitOrderToExchange o)
                    let transaction: Transaction = { OrderIds = orders |> List.map (fun o -> o.OrderId); TransactionStatus = PartiallyFulfilled  }
                    saveTransactionToDB transaction
