module QueueSender

open Azure.Messaging.ServiceBus
open Models

type ServiceBusMessageType =
    | StrategyChange
    | TopPairChange
    | CompletedOrder of Order

let addAttributes (message : ServiceBusMessage, order : Order) = 
    message.ApplicationProperties.Add("OrderType", order.OrderType.ToString())
    message.ApplicationProperties.Add("Price", order.Price.ToString())
    message.ApplicationProperties.Add("Size", order.Size.ToString())

let sendMessageAsync (messageType : ServiceBusMessageType) = async {

    let client = ServiceBusClient("Endpoint=sb://arbitrage-gainer-servicebus.servicebus.windows.net/;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=jxnoEqHx0sJ0Bsi+C/yGFFIANYYQWRniE+ASbJFobaA=")
    let queueName =
        match messageType with
        | StrategyChange -> "strategy-queue"
        | TopPairChange -> "pair-queue"
        | CompletedOrder order -> "order-queue"
     
    let sender = client.CreateSender(queueName)
    let message = new ServiceBusMessage()

    let messageName =
        match messageType with
        | StrategyChange -> "Strategy"
        | TopPairChange -> "Pair"
        | CompletedOrder order -> "Order"

    message.ApplicationProperties.Add("Name", messageName)

    match messageType with
    | CompletedOrder order -> addAttributes (message, order)
    | _ -> ()

    sender.SendMessageAsync(message).Wait()

    sender.DisposeAsync().AsTask().Wait()
    client.DisposeAsync().AsTask().Wait()
    
}

