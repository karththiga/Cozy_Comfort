# Cozy Comfort - Service-Oriented Commerce (SOC) Assignment

## 1) Project Overview
This solution demonstrates a **Service-Oriented Commerce (SOC)** application for Cozy Comfort's blanket supply chain:
- **Manufacturer** (production and factory stock)
- **Distributor** (warehouse and logistics stock)
- **Seller** (storefront stock and customer fulfillment)

The solution is split into two applications:
1. **SOC_CozyComfort_API** - Web API services (inventory CRUD by role)
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
- `GET /api/inventory/{role}`
- `GET /api/inventory/{role}/{id}`
- `POST /api/inventory/{role}`
- `PUT /api/inventory/{role}/{id}`
- `DELETE /api/inventory/{role}/{id}`

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
