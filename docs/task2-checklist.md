# Task 2 (LO2, LO3) Submission Checklist - Cozy Comfort

Use this checklist before final PDF submission.

## A) SOC Design Evidence
- [x] Clear service boundaries (Auth, Inventory, Order Requests, Notifications)
- [x] Diagram showing client-service-database flow
- [x] Sequence diagram for real business flow (seller shortage escalation)
- [x] Logical design showing controller responsibilities
- [x] Data model summary for core tables

## B) Development Evidence
- [x] API implemented in .NET (`SOC_CozyComfort_API`)
- [x] Client app consuming API (`SOC_Cozy_Comfort_Client`)
- [x] Role-based dashboards (Manufacturer, Distributor, Seller)
- [x] Inventory CRUD per role
- [x] Request workflow services (seller-distributor-manufacturer)
- [x] Notification service integration
- [x] Signup/login flow through API

## C) Coding Standards, Reusability, Maintainability
- [x] Client API calls centralized in service classes
- [x] Validation and error messages returned from API
- [x] Config-based API base URL
- [x] Role checks consistently applied
- [x] Separation between views, controllers, and service classes

## D) What to include in your report for higher marks
- [ ] Explain design trade-offs (why this boundary, what alternatives)
- [ ] Add 2–3 screenshots per major module (inventory, requests, notifications)
- [ ] Add code snippets proving reuse (client service wrappers)
- [ ] Explain maintainability strategy (where future changes go)
- [ ] Explain scalability strategy (independent endpoint scaling, DB indexing plan)

## E) Task 2 Risk Gaps to avoid
- [ ] Outdated diagrams that don't match code
- [ ] Missing API docs for non-inventory modules
- [ ] No explanation of reusability/maintainability decisions
- [ ] No evidence of end-to-end flow (customer -> seller -> distributor -> manufacturer)

## F) Quick file references
- Architecture diagrams: `docs/architecture.md`
- API documentation: `docs/api-reference.md`
- Project overview: `README.md`
