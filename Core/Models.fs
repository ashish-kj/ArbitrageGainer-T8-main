module Models

type Strategy = {
    CurrencyNumber: int
    MinSpread: float
    MinProfit: float
    MaxTransaction: float
    MaxDailyTransaction: float
}

type Quote = { 
    Pair: string
    ExchangeId: decimal
    BidPrice: float
    AskPrice: float
    BidSize: float
    AskSize: float
    Timestamp: decimal 
}

type Opportunity = {
    CurrencyPair: string
    OpportunityNumber: int
}

type OrderStatus =
    | Created
    | Fulfilled
    | Partial
    | NotFilled

type OrderType = 
    | Buy
    | Sell
    | BuyRemaining
    | SellRemaining

type Order = {
    OrderId: string
    ExchangeId: decimal
    Price: float
    Pair: string
    Size: float
    RealizedSize: float
    OrderType: OrderType
    OrderStatus: OrderStatus
    RelatedOrderId: string // buyOrder's relatedOrderId is sellOrder's orderId, and sellOrder's relatedOrderId is buyOrder's orderId; buyRemainingOrder's relatedOrderId is buyOrder's orderId; sellRemainingOrder's relatedOrderId is sellOrder's orderId
}

type TransactionStatus =
    | FullyFulfilled
    | PartiallyFulfilled
    | OneSideFulfilled

type Transaction = {
    OrderIds: string list
    TransactionStatus: TransactionStatus
}

// accumulate profit for completed transactions
type CompletedTransaction = {
    BuyAmount: decimal;
    BuyPrice: decimal;
    SellAmount: decimal;
    SellPrice: decimal;
}
