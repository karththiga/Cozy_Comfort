# API Reference - Cozy Comfort SOC Services

Base URL: configured by environment (example: `https://localhost:44377/`)

---

## 1) Auth Service (`/api/auth`)

### `POST /api/auth/login`
Authenticate user and resolve role.

**Request**
```json
{ "UserName": "m_admin", "Password": "M@123" }
```

**Responses**
- `200 OK` with `{ UserName, Role, Message }`
- `400 BadRequest` invalid payload
- `401 Unauthorized` invalid credentials

### `POST /api/auth/signup`
Create new user account request (persisted through API logic).

**Request**
```json
{
  "FullName": "Jane Doe",
  "Email": "jane@company.com",
  "UserName": "jane_admin",
  "Role": "Distributor",
  "Password": "Strong@123"
}
```

**Responses**
- `200 OK` signup success
- `400 BadRequest` validation/duplicate errors

---

## 2) Inventory Service (`/api/inventory`)

Supported roles: `Manufacturer`, `Distributor`, `Seller`

### `GET /api/inventory/{role}`
Get all inventory by role.

### `GET /api/inventory/{role}/{id}`
Get inventory row by id.

### `POST /api/inventory/{role}`
Create inventory row.

### `PUT /api/inventory/{role}/{id}`
Update inventory row.

### `DELETE /api/inventory/{role}/{id}`
Delete inventory row.

**Common responses**
- `200 OK`
- `400 BadRequest` invalid role/payload
- `404 NotFound` for missing id

---

## 3) Order Request Service (`/api/orderrequests`)

### Read Boards
- `GET /api/orderrequests/incoming/{role}`
- `GET /api/orderrequests/outgoing/{role}`

### Seller -> Distributor
- `POST /api/orderrequests/seller-to-distributor`

### Distributor Actions
- `POST /api/orderrequests/distributor/escalate/{requestId}`
- `POST /api/orderrequests/distributor/fulfill/{requestId}`
- `POST /api/orderrequests/distributor/cancel/{requestId}`

### Manufacturer Actions
- `POST /api/orderrequests/manufacturer/start-production/{requestId}`
- `POST /api/orderrequests/manufacturer/dispatch/{requestId}`
- `POST /api/orderrequests/manufacturer/cancel/{requestId}`

### Seller Action
- `POST /api/orderrequests/seller/cancel/{requestId}`

**Common responses**
- `200 OK`
- `400 BadRequest` invalid payload
- `404 NotFound` request id not found

---

## 4) Notification Service (`/api/notifications`)

### `GET /api/notifications/{role}`
Get all notifications for a role.

### `POST /api/notifications/{role}/read/{id}`
Mark a notification as read.

---

## 5) Client Integration Map
- `AuthApiClient` -> `/api/auth/*`
- `InventoryApiClient` -> `/api/inventory/*`
- `OrderRequestApiClient` -> `/api/orderrequests/*`
- `NotificationApiClient` -> `/api/notifications/*`

This mapping demonstrates clear separation of concerns and reusable API client wrappers.
