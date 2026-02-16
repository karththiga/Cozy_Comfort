# Architecture & Design Diagrams (Updated for Submission)

## 1) High-Level SOC Component Diagram
```mermaid
flowchart LR
    U[End Customer] --> S[Seller]
    S --> C[Client MVC App\nSOC_Cozy_Comfort_Client]

    C -->|HTTP JSON| A[API Gateway Layer\nSOC_CozyComfort_API]
    A --> INV[Inventory Service]
    A --> ORQ[Order Request Service]
    A --> AUT[Auth Service]
    A --> NOTI[Notification Service]

    INV --> DB[(SQL Server: CozyComfortDb)]
    ORQ --> DB
    AUT --> DB
    NOTI --> DB
```

## 2) Service Interaction Flow (Seller Shortage Scenario)
```mermaid
sequenceDiagram
    participant Customer
    participant SellerUI as Seller (MVC Client)
    participant API as Web API
    participant DB as SQL Server

    Customer->>SellerUI: Place order (SKU, Qty)
    SellerUI->>API: GET /api/inventory/Seller
    API->>DB: Read seller stock
    DB-->>API: Inventory rows
    API-->>SellerUI: Current stock

    alt Seller stock available
        SellerUI->>API: PUT /api/inventory/Seller/{id}
        API->>DB: Update quantity
        DB-->>API: Success
        API-->>SellerUI: Order fulfilled from seller
    else Seller stock unavailable
        SellerUI->>API: POST /api/orderrequests/seller-to-distributor
        API->>DB: Create request + notification
        DB-->>API: Success
        API-->>SellerUI: Request sent to distributor
    end
```

## 3) Logical Service Design
```mermaid
classDiagram
    class AuthController {
      +Signup(request)
      +Login(request)
    }

    class InventoryController {
      +GetByRole(role)
      +GetById(role,id)
      +Create(role,item)
      +Update(role,id,item)
      +Delete(role,id)
    }

    class OrderRequestsController {
      +Incoming(role)
      +Outgoing(role)
      +CreateSellerToDistributor(request)
      +DistributorEscalate(requestId, action)
      +DistributorFulfill(requestId, action)
      +ManufacturerStartProduction(requestId, action)
      +ManufacturerDispatch(requestId, action)
    }

    class NotificationsController {
      +GetByRole(role)
      +MarkRead(role,id)
    }

    class ClientHomeController {
      +Login()
      +Signup()
      +Manufacturer()
      +Distributor()
      +Seller()
      +SellerRequests()
      +DistributorRequests()
      +ManufacturerRequests()
      +Notifications()
    }

    ClientHomeController --> AuthController : via AuthApiClient
    ClientHomeController --> InventoryController : via InventoryApiClient
    ClientHomeController --> OrderRequestsController : via OrderRequestApiClient
    ClientHomeController --> NotificationsController : via NotificationApiClient
```

## 4) Data Design (Core Tables)
- `Roles` (`Id`, `RoleName`)
- `Users` (`Id`, `UserName`, `Password`, `RoleId`) + optional schema extension support handled safely by signup logic
- `InventoryItems` (`Id`, `RoleName`, `Sku`, `Name`, `Quantity`, `Location`, `LastUpdated`)
- `OrderRequests` (`Id`, `RequestType`, `RequestedByRole`, `RequestedToRole`, `RequestedByUser`, `Sku`, `BlanketName`, `Quantity`, `Status`, `Notes`, timestamps)
- `Notifications` (`Id`, `RecipientRole`, `Title`, `Message`, `NotificationType`, `IsRead`, `RelatedRequestId`, `CreatedAt`)

## 5) Maintainability and Reusability Decisions
1. **Service boundaries by domain**: auth, inventory, requests, notifications are separated at controller/repository layer.
2. **Client adapters**: all API calls are centralized in service classes (`AuthApiClient`, `InventoryApiClient`, etc.), reducing duplication.
3. **Role isolation**: role checks are consistently enforced in client and API paths.
4. **Schema compatibility**: signup supports legacy DB schema safely while maintaining upgrade compatibility.
5. **Config-driven integration**: API base URL is externalized through configuration.

## 6) Scalability Notes
- API services can be scaled horizontally behind IIS/Web farm.
- Request-heavy endpoints (`/api/orderrequests/*`, `/api/inventory/*`) are independently optimizable.
- Database indexing can be extended on SKU, RoleName, and request status fields as usage grows.
