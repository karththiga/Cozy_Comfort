# Architecture & Design Diagrams

## 1) High-Level SOC Component Diagram
```mermaid
flowchart LR
    U[User] --> C[Client MVC App\nSOC_Cozy_Comfort_Client]
    C -->|HTTP JSON| A[Inventory API\nSOC_CozyComfort_API]
    A --> R[(In-Memory Inventory Repository)]

    C --> CAuth[Role-based Session/Auth]
    C --> CDash[Manufacturer/Distributor/Seller Dashboards]

    A --> ACtrl[InventoryController]
    A --> AModel[InventoryItemDto]
```

## 2) Role Flow Diagram (Login to CRUD)
```mermaid
sequenceDiagram
    participant User
    participant Client as MVC Client
    participant API as Inventory API

    User->>Client: Login (username/password/role)
    Client-->>User: Role dashboard

    User->>Client: Open dashboard inventory
    Client->>API: GET /api/inventory/{role}
    API-->>Client: Inventory items JSON
    Client-->>User: Inventory table + summary cards

    User->>Client: Add/Edit/Delete item
    Client->>API: POST/PUT/DELETE /api/inventory/{role}[/{id}]
    API-->>Client: Success/Failure
    Client->>API: GET /api/inventory/{role}
    API-->>Client: Updated list
    Client-->>User: Refreshed UI
```

## 3) Client Logical Design
```mermaid
classDiagram
    class HomeController {
      +Login()
      +Manufacturer()
      +Distributor()
      +Seller()
      +AddInventory()
      +EditInventory()
      +DeleteInventory()
      -RenderDashboard()
      -IsAuthorizedFor()
    }

    class InventoryApiClient {
      +GetByRole(role)
      +GetById(role,id)
      +Create(role,item)
      +Update(role,id,item)
      +Delete(role,id)
    }

    class RoleDashboardViewModel {
      +Role
      +LoggedInUser
      +Items
      +NewItem
    }

    class InventoryItem {
      +Id
      +Sku
      +Name
      +Quantity
      +Location
      +LastUpdated
    }

    HomeController --> InventoryApiClient
    HomeController --> RoleDashboardViewModel
    RoleDashboardViewModel --> InventoryItem
```

## 4) API Logical Design
```mermaid
classDiagram
    class InventoryController {
      +GetByRole(role)
      +GetById(role,id)
      +Create(role,item)
      +Update(role,id,item)
      +Delete(role,id)
    }

    class InventoryRepository {
      +IsValidRole(role)
      +GetByRole(role)
      +GetById(role,id)
      +Add(role,item)
      +Update(role,id,item)
      +Delete(role,id)
    }

    class InventoryItemDto {
      +Id
      +Sku
      +Name
      +Quantity
      +Location
      +LastUpdated
    }

    InventoryController --> InventoryRepository
    InventoryRepository --> InventoryItemDto
```

## 5) Maintainability & Reusability Design Decisions
- Service interaction is centralized in `InventoryApiClient`.
- API CRUD is centralized in a single controller + repository.
- Role-aware behavior is consolidated through helper methods (`RenderDashboard`, `IsAuthorizedFor`).
- View models separate UI concerns from transport DTOs.
- Config-driven API base URL allows environment portability without code changes.
