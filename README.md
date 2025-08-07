# Travel Agency API – ADO.NET with SqlConnection and SqlCommand

This project was created as part of my studies in computer science at **Polish-Japanese Academy of Information Technology.**  
The goal was to build a REST API for a travel agency database using ADO.NET and raw SQL commands, without using any ORM.

The API exposes endpoints to manage clients, trips, countries, and their relationships.  
The project focuses on **backend** logic only, there is no frontend or user interface.

## Endpoints

### Trips listing

Returns a list of all available trips with basic details like ID, name, description, date range, max participants, and related countries.  

### Client's trips

Returns all trips linked to a specific client by client ID.  
Includes trip details and registration/payment info.  
Handles cases when the client does not exist or has no trips.

### Adding a new client

Creates a new client record from request data (`FirstName`, `LastName`, `Email`, `Telephone`, `Pesel`).  
Validates required fields and formats.  
Returns appropriate status codes and the new client ID.

### Registering a client for a trip

Registers a client to a trip given client ID and trip ID.  
Checks if client and trip exist and verifies trip capacity.  
Inserts a record into `Client_Trip` with the current date as `RegisteredAt`.  
Returns suitable status codes and messages.

### Removing a client from a trip

Deletes a client’s registration from a trip by client ID and trip ID.  
Checks if registration exists before deleting.  
Returns appropriate status codes and messages.

---

All queries are parameterized to prevent SQL injection.  

# CW-7-s31552
