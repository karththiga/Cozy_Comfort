# Cozy Comfort - Service-Oriented Commerce (SOC) Assignment

## 1) Project Overview
This solution demonstrates a **Service-Oriented Commerce (SOC)** application for Cozy Comfort's blanket supply chain:
- **Manufacturer** (production and factory stock)
- **Distributor** (warehouse and logistics stock)
- **Seller** (storefront stock and customer fulfillment)

The solution is split into two applications:
1. **SOC_CozyComfort_API** - Web API services (inventory CRUD by role) backed by Local SQL Server
2. **SOC_Cozy_Comfort_Client** - MVC client application consuming the API

---

## 2) SOC Architecture Goals Covered
- Clear **service boundary** via API endpoints
- **Role-based client dashboards** with inventory management
- **CRUD operations** routed through API (client does not directly mutate data store)
- Separation of concerns using models, controllers, and service classes
- Config-driven integration (`InventoryApiBaseUrl`)

See detailed diagrams and API documentation in:
- [`docs/architecture.md`](docs/architecture.md)
- [`docs/api-reference.md`](docs/api-reference.md)
- [`docs/task2-checklist.md`](docs/task2-checklist.md)

---

## 3) Implemented Functionalities
### Authentication & Role Access (Client)
- Common login page for all roles
- Role-based dashboard navigation
- Session-based role authorization

### Inventory Management (Per Role)
- List inventory
- Add inventory item
- Edit inventory item
- Delete inventory item
- Top-level inventory summary cards

### API Services
- Inventory endpoints (`/api/inventory/*`)
- Authentication endpoints (`/api/auth/login`, `/api/auth/signup`)
- Order request workflow endpoints (`/api/orderrequests/*`)
- Notification endpoints (`/api/notifications/*`)

---

## 4) Demo Credentials
Use these in client login:
- Manufacturer: `m_admin / M@123`
- Distributor: `d_admin / D@123`
- Seller: `s_admin / S@123`

---

## 5) Run Instructions (Visual Studio)
1. Open `SOC_CozyComfort_API.sln`
2. Set both projects as startup projects:
   - `SOC_CozyComfort_API`
   - `SOC_Cozy_Comfort_Client`
3. Ensure client `Web.config` has correct API URL:
   - `InventoryApiBaseUrl` should match API local URL
4. Run solution
5. Login on client and test CRUD flows

---

## 6) Maintainability Notes
- API and client are independently evolvable
- Endpoint integration centralized in `InventoryApiClient`
- Models are explicit and typed
- Role checks and dashboard rendering are encapsulated in controller helpers
- Documentation and design diagrams are included for easier onboarding and extension


## 7) Database
- API now uses **Local SQL Server** instead of hardcoded in-memory values.
- Connection string: `CozyComfortDb` in `SOC_CozyComfort_API/Web.config`.
- Tables created/seeded automatically on API startup via `DbInitializer`.
- Optional SQL script for manual setup: `database/01_create_tables_and_seed.sql`.


## 8) Troubleshooting CRUD persistence
If you can create/update in UI but do not see changes in SSMS, verify SSMS is connected to **(localdb)\MSSQLLocalDB** (the same instance configured by `CozyComfortDb` in `SOC_CozyComfort_API/Web.config`).

Run:
```sql
SELECT DB_NAME() AS CurrentDb;
SELECT COUNT(*) AS TotalItems FROM dbo.InventoryItems;
SELECT TOP 20 * FROM dbo.InventoryItems ORDER BY Id DESC;
```

Also ensure API is running and client points to correct API base URL (`InventoryApiBaseUrl`).
