# API Reference - Inventory Service

Base Route: `/api/inventory`

## Roles Supported
- `Manufacturer`
- `Distributor`
- `Seller`

---

## 1) Get Inventory by Role
**GET** `/api/inventory/{role}`

### Response
`200 OK`
```json
[
  {
    "Id": 1,
    "Sku": "CC-WOOL-QUEEN",
    "Name": "Wool Queen Blanket",
    "Quantity": 5420,
    "Location": "Factory A",
    "LastUpdated": "2026-02-10T10:30:00"
  }
]
```

---

## 2) Get Inventory Item by Id
**GET** `/api/inventory/{role}/{id}`

### Responses
- `200 OK` with item
- `404 Not Found` if id missing

---

## 3) Create Inventory Item
**POST** `/api/inventory/{role}`

### Request Body
```json
{
  "Sku": "CC-NEW-MODEL",
  "Name": "New Blanket",
  "Quantity": 100,
  "Location": "Warehouse-A"
}
```

### Responses
- `200 OK` with created item
- `400 Bad Request` for invalid role/payload

---

## 4) Update Inventory Item
**PUT** `/api/inventory/{role}/{id}`

### Request Body
```json
{
  "Sku": "CC-NEW-MODEL",
  "Name": "New Blanket Updated",
  "Quantity": 140,
  "Location": "Warehouse-B"
}
```

### Responses
- `200 OK`
- `404 Not Found`
- `400 Bad Request`

---

## 5) Delete Inventory Item
**DELETE** `/api/inventory/{role}/{id}`

### Responses
- `200 OK`
- `404 Not Found`
- `400 Bad Request`

---

## Integration Notes
- Client consumes endpoints via `InventoryApiClient` using `InventoryApiBaseUrl` from `Web.config`.
- Ensure API and client run together with matching URL/port configuration.
