## 🛒 E-commerce Lite
Backend API for a lightweight e-commerce platform built with C# ASP.NET EF MS SQL Kafka. Implements product management, order processing with saga-pattern compensation, inventory reservation, payment simulation, and real-time event notifications via Kafka.

---

## 🚀 Features

- **Product Management**
  - Create and retrieve products
  - Redis-based caching for product listings 
  - Kafka event publishing on product creation

- **Order Processing (Saga Pattern)**
  - Place orders with automatic multi-step orchestration
  - Inventory reservation → Payment processing → Confirmation
  - Automatic compensation (stock release) on payment failure
  - Order status tracking: Pending → InventoryReserved → PaymentProcessing → Completed / Failed / Cancelled

- **Inventory Management**
  - Add, reserve, release, and confirm stock
  - Available quantity computed from total minus reserved
  - Full stock lifecycle per product

- **Payment Simulation**
  - Processes payments with 90% success rate simulation
  - Payment history queryable per order
  - Stores payment status and timestamp

- **Notification Service**
  - Kafka consumer listening to `product-created` and `order-created` topics
  - Formatted console output for real-time event visibility

- **API Gateway**
  - Ocelot-based gateway routing all external traffic
  - Single entry point at port 5555
  - Routes to all downstream services

## ⚙️ Tech Stack

| Component | Technology/Library |
|-----------|-------------------|
| Web Framework | ASP.NET Core 8 |
| DB Access | Entity Framework Core |
| Database | MS SQL Server |
| Cache | Redis (IDistributedCache) |
| Message Broker | Apache Kafka (Confluent.Kafka) |
| API Gateway | Ocelot |
| Documentation | Swagger |

## 🚀 Quick Start

**Steps:**

1. Clone the repository
```bash
git clone https://github.com/your-username/E-commerce-Lite.git
cd E-commerce-Lite
```

2. Make sure you have MS SQL Server, Redis, and Kafka running locally (or via Docker).

3. Apply EF migrations for each service that uses a database:
```bash
cd ProductService && dotnet ef database update
cd ../OrderService && dotnet ef database update
cd ../InventoryService && dotnet ef database update
cd ../PaymentService && dotnet ef database update
```

4. Run all services (in separate terminals or with your IDE):
```bash
dotnet run --project ApiGatawey
dotnet run --project ProductService
dotnet run --project OrderService
dotnet run --project InventoryService
dotnet run --project PaymentService
dotnet run --project NotificationService
```

5. Access via gateway or directly:
   - API Gateway: `http://localhost:5555/gateway/...`
   - ProductService: `http://localhost:5281/swagger`
   - OrderService: `http://localhost:5025/swagger`
   - InventoryService: `http://localhost:5194/swagger`
   - PaymentService: `http://localhost:5065/swagger`

## 📝 API Endpoints

### API Gateway (port 5555)

All downstream routes are accessible through the Ocelot gateway:

| Method | Gateway Route | Downstream Service |
|--------|-------------|-------------------|
| GET, POST | `/gateway/products` | ProductService |
| GET | `/gateway/products/{id}` | ProductService |
| GET, POST | `/gateway/orders` | OrderService |
| GET | `/gateway/orders/{id}` | OrderService |
| GET, POST | `/gateway/inventory` | InventoryService |
| POST | `/gateway/inventory/stock` | InventoryService |
| GET | `/gateway/inventory/{productId}` | InventoryService |
| GET | `/gateway/payments/order/{orderId}` | PaymentService |

---

### ProductService (port 5281)

- `POST /api/product` — Create a new product (publishes `product-created` to Kafka, invalidates cache)
- `GET /api/product` — List all products (Redis-cached, 5 min TTL)
- `GET /api/product/{id}` — Get product by ID

### OrderService (port 5025)

- `POST /api/order` — Place a new order (orchestrates inventory + payment saga, publishes `order-created` to Kafka)
- `GET /api/order` — List all orders
- `GET /api/order/{id}` — Get order by ID

### InventoryService (port 5194)

- `GET /api/inventory` — List all inventory items
- `GET /api/inventory/{productId}` — Get stock for a product
- `POST /api/inventory/stock` — Add stock for a product
- `POST /api/inventory/reserve` — Reserve stock (called by OrderService)
- `POST /api/inventory/release` — Release reserved stock on failure
- `POST /api/inventory/confirm` — Confirm stock deduction on payment success

### PaymentService (port 5065)

- `POST /api/payment/process` — Process a payment (90% simulated success rate)
- `GET /api/payment/{id}` — Get payment by ID
- `GET /api/payment/order/{orderId}` — Get all payments for an order

## 🔄 Order Flow (Saga)

```
Client → POST /api/order
  1. Fetch product info from ProductService
  2. Create order (Status: Pending)
  3. Reserve inventory → success? (Status: InventoryReserved)
     └── failure → cancel order (Status: Cancelled)
  4. Process payment (Status: PaymentProcessing)
     └── failure → release inventory → (Status: Failed)
  5. Confirm inventory deduction
  6. Complete order (Status: Completed)
  7. Publish "order-created" event to Kafka
     └── NotificationService logs the event
```

## 📦 Project Structure

```
E-commerce-Lite/
├── ApiGatawey/
│   ├── ocelot.json                        # Route definitions for all services
│   └── Program.cs                         # Ocelot middleware setup
├── ProductService/
│   ├── Controllers/
│   │   └── ProductController.cs           # Create, GetAll (cached), GetById
│   ├── Data/
│   │   └── AppDbContext.cs                # EF Core DbContext
│   ├── DTOs/
│   │   └── CreateProductDto.cs            # Create product request body
│   ├── Kafka/
│   │   └── KafkaProducer.cs               # Publishes product-created events
│   ├── Models/
│   │   └── Product.cs                     # Product entity
│   └── Program.cs                         # DI, Redis, Kafka, EF setup
├── OrderService/
│   ├── Controllers/
│   │   └── OrderController.cs             # Saga orchestrator: create, get orders
│   ├── Data/
│   │   └── AppDbContext.cs                # EF Core DbContext
│   ├── DTOs/
│   │   └── CreateOrderDto.cs              # Place order request body
│   ├── HttpClients/
│   │   ├── ProductHttpClient.cs           # HTTP call to ProductService
│   │   ├── InventoryHttpClient.cs         # HTTP calls to InventoryService
│   │   └── PaymentHttpClient.cs           # HTTP call to PaymentService
│   ├── Kafka/
│   │   └── KafkaProducer.cs               # Publishes order-created events
│   ├── Models/
│   │   ├── Order.cs                       # Order entity + OrderStatus enum
│   │   ├── ProductInfo.cs                 # DTO from ProductService response
│   │   └── PaymentResult.cs               # DTO from PaymentService response
│   └── Program.cs                         # DI, HTTP clients, Kafka, EF setup
├── InventoryService/
│   ├── Controllers/
│   │   └── InventoryController.cs         # Stock CRUD + reserve/release/confirm
│   ├── Data/
│   │   └── AppDbContext.cs                # EF Core DbContext
│   ├── DTOs/
│   │   └── ReserveStockDto.cs             # Reserve/release/confirm request bodies
│   ├── Models/
│   │   └── InvenoryItem.cs                # Inventory entity with AvailableQuantity
│   └── Program.cs                         # DI, EF setup
├── PaymentService/
│   ├── Controllers/
│   │   └── PaymentController.cs           # Process, get payments
│   ├── Data/
│   │   └── AppDbContext.cs                # EF Core DbContext
│   ├── DTOs/
│   │   └── ProcessPaymentDto.cs           # Payment request body
│   ├── Models/
│   │   └── Payment.cs                     # Payment entity + PaymentStatus enum
│   └── Program.cs                         # DI, EF setup
├── NotificationService/
│   ├── Kafka/
│   │   └── NotificationConsumer.cs        # Consumes product-created & order-created
│   └── Program.cs                         # Background service registration
└── MicroService_V2.sln                    # Solution file
```

## 📄 License
This project is licensed under the MIT License.
