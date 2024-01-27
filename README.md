# ArbitrageGainer-T8

ArbitrageGainer is a personal trading application that leverages arbitrage opportunities in the cryptocurrency market.

## Project Overview

The ArbitrageGainer application aims to identify and capitalize on price differences in cryptocurrencies across different exchanges. It follows a set of well-defined workflows to collect real-time market data, update transaction volumes, compare minimal profit, emit orders, and handle fulfillment and communication.

## Docker
- `docker pull tengrr/18656_8`
- `docker run -p 8080:8080 tengrr/18656_8`

## API Documentation
Click [here](https://documenter.getpostman.com/view/27006922/2s9YeHbB85)

## Project Structure

```
.
├── ArbitrageGainer-T8.fsproj
├── Bitfinex.txt
├── Bitstamp.txt
├── Core
│   └── Models.fs
├── Services
│   ├── CurrencyPairCalculation.fs
│   ├── HistoricalOpportunityCalculation.fs
│   ├── OrderManagement.fs
│   └── RealTimeTrading.fs
├── InfraStructure
│   ├── Daos
│   │   ├── CurrencyPairDao.fs
│   │   ├── OpportunityDao.fs
│   │   ├── OrderDao.fs
│   │   ├── StrategyDao.fs
│   │   ├── ProfitDao.fs
│   │   ├── OutflowDao.fs
│   │   ├── InflowDao.fs
│   │   └── TransactionDao.fs
│   ├── Services
│   |   ├── CurrencyPairService.fs
│   |   ├── OrderManagement.fs
│   |   ├── HistoricalOpportunityService.fs
│   |   ├── RealTimeDataWebSocket.fs
│   |   └── ProfitLossService.fs
│   └── Controllers
│       ├── CurrencyPairController.fs
│       ├── OpportunityController.fs
│       ├── ProfitLossController.fs
│       ├── StrategyController.fs
│       └── TradingController.fs
├── Kraken.txt
├── Program.fs
├── README.md
├── historicalData.txt


```


## Domain

### Historical Data Domain Service

### Entry Point Function :
- `Program.fs`


### Side-Effect Areas and Source Files

1. **Accept user input**
   - File: `InfraStructure/Controllers/Strategy.fs`

2. **Retrieval of cross-traded currency pairs**
   - File: `InfraStructure/Controllers/CurrencyPairController.fs` and `Infrastructure/Services/CurrencyPairService.fs`

3. **Historical arbitrage opportunities calculation**
   - File: `Infrastructure/Controllers/OpportunityController.fs` and `Infrastructure/Services/HistoricalOpportunityService.fs`

4. **Real-time market data retrieval**
   - File: `InfraStructure/Controllers/TradingController.fs` and `Infrastructure/Services/RealTimeDataWebSocket.fs`

5. **Order management**
   - File: `InfraStructure/Services/OrderManagement.fs`

6. **Profit and Loss**
   - File: `InfraStructure/Controllers/ProfitLossController.fs` and `Infrastructure/Services/ProfitLossService.fs`

7. **Data persistence**
   - Files:
     - `InfraStructure/Daos/CurrencyPairDao.fs`
     - `InfraStructure/Daos/OpportunityDao.fs`
     - `InfraStructure/Daos/OrderDao.fs`
     - `InfraStructure/Daos/StrategyDao.fs`
     - `InfraStructure/Daos/TransactionDao.fs`
     - `InfraStructure/Daos/ProfitDao.fs`
     - `InfraStructure/Daos/InflowDao.fs`
     - `InfraStructure/Daos/OutflowDao.fs`

8. **Error handling**
     - `Program.fs`: Main entry point of the application.
     - `InfraStructure/Controllers/CurrencyPairController.fs`: Handles operations related to currency pairs.
     - `InfraStructure/Controllers/OpportunityController.fs`: Handles operations related to historical data retrieval opportunities.
     - `InfraStructure/Controllers/RealTimeDataWebSocket.fs`: Handles real-time data retrieval.
     - `InfraStructure/Controllers/StrategyController.fs`: Handles operations related to strategies.
     - `InfraStructure/Controllers/TradingController.fs`: Handles operations related to trading.
     - `InfraStructure/Daos/CurrencyPairDao.fs`: Data access object for currency pairs.
     - `InfraStructure/Daos/OpportunityDao.fs`: Data access object for opportunities.
     - `InfraStructure/Daos/OrderDao.fs`: Data access object for orders.
     - `InfraStructure/Daos/StrategyDao.fs`: Data access object for strategies.
     - `InfraStructure/Daos/TransactionDao.fs`: Data access object for transactions.
     - `Services/CurrencyPairCalculation.fs`: Service for calculating currency pairs.
     - `Services/HistoricalOpportunityCalculation.fs`: Service for calculating historical opportunities.
     - `Services/OrderManagement.fs`: Service for managing orders.
     - `Services/RealTimeTrading.fs`: Service for real-time trading.
   


### Collect Market Data and Identify Arbitrage Opportunities

#### Source Code :
- `Models.fs`
- `InfraStructure/Controllers/CurrencyPairController.fs`
- `InfraStructure/Controllers/OpportunityController.fs`
- `InfraStructure/Daos/CurrencyPairDao.fs`
- `Services/CurrencyPairCalculation.fs`
- `Services/HistoricalOpportunityCalculation.fs`

### Update Transactions Volume

#### Source Code:
- `Models.fs`
- `InfraStructure/Daos/TransactionDao.fs`

### Compare Minimal Profit and Maximal Transaction Amount

#### Source Code:
- `Models.fs`
- `InfraStructure/Controllers/StrategyController.fs`
- `InfraStructure/Daos/StrategyDao.fs`
- `Services/RealTimeTrading.fs`

### Emit Orders

#### Source Code:
- `Models.fs`
- `InfraStructure/Daos/OrderDao.fs`
- `Services/OrderManagement.fs`
- `Services/RealTimeTrading.fs`
