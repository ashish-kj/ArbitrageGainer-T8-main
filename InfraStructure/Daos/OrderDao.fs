module OrderDao

open Models
open System
open Azure
open Azure.Data.Tables

let storageConnString = "DefaultEndpointsProtocol=https;AccountName=arbitrage-gainer-storage;AccountKey=yLkIl5Kt7oFwc5XSzxPjn52HOLNBYnBhzilMX2z5uK8XvfRWAeTQPpApWBMhzTYAFP2uac2Nf9cTACDbgOCeZw==;TableEndpoint=https://arbitrage-gainer-storage.table.cosmos.azure.com:443/;"
let tableClient = TableServiceClient storageConnString

let table = tableClient.GetTableClient "orders"

let createOrderTable = table.CreateIfNotExists () |> ignore

type OrderEntity (order: Order) =
    interface ITableEntity with
        member val ETag = ETag "" with get, set
        member val PartitionKey = order.Pair with get, set
        member val RowKey = string order.OrderId with get, set
        member val Timestamp = Nullable() with get, set

    new() = OrderEntity(Unchecked.defaultof<Order>)
    member val OrderId = order.OrderId with get, set
    member val ExchangeId = order.ExchangeId with get, set
    member val Price = order.Price with get, set
    member val Pair = order.Pair with get, set
    member val Size = order.Size with get, set
    member val RealizedSize = order.RealizedSize with get, set
    member val OrderType = order.OrderType.ToString() with get, set
    member val OrderStatus = order.OrderStatus.ToString() with get, set
    member val RelatedOrderId = order.RelatedOrderId with get, set

let addOrder (order: Order) =
    let entity = OrderEntity(order)
    table.AddEntity entity

let getOrder (orderId: string) = 
    let query = $"RowKey eq '{orderId}'"
    table.Query<OrderEntity>(query)
    |> Seq.tryHead
    |> Option.map (fun entity -> 
        // Map back to Order type
        {
            OrderId = entity.OrderId
            ExchangeId = entity.ExchangeId
            Price = entity.Price
            Pair = entity.Pair
            Size = entity.Size
            RealizedSize = entity.RealizedSize
            OrderType = 
                match entity.OrderType with
                | "Buy" -> Buy
                | "Sell" -> Sell
                | "BuyRemaining" -> BuyRemaining
                | "SellRemaining" -> SellRemaining
                | _ -> failwith "Invalid OrderType"
            OrderStatus = 
                match entity.OrderStatus with
                | "Created" -> Created
                | "Fulfilled" -> Fulfilled
                | "Partial" -> Partial
                | "NotFilled" -> NotFilled
                | _ -> failwith "Invalid OrderStatus"
            RelatedOrderId = entity.RelatedOrderId
        }
    ) 

let updateOrder (updatedOrder: Order) =
    let entity = new OrderEntity(updatedOrder)
    table.UpsertEntity(entity, TableUpdateMode.Replace)
