# Postman reviewer notes

Import `SupportManagementSystem.postman_collection.json` into Postman and run its seven folders in order against a freshly seeded Development database. The default `baseUrl` is `http://localhost:5052`.

The collection exercises:

- JWT login for all three seeded roles and `/api/auth/me`
- an anonymous `401 Unauthorized` check
- admin user lookup and creation of a unique second customer
- customer ticket creation, query/pagination, detail, and comments
- admin assignment and priority change
- agent status work, comments, time tracking, and timeline
- strict foreign-customer list/detail/comment isolation
- customer close and admin dashboard

Login response scripts keep tokens as collection variables in the active Postman session. The exported file contains empty token variables. If you export the collection after running it, clear `adminToken`, `agentToken`, `customerToken`, and `customer2Token` first.

Some mutating endpoints legitimately return either `200 OK` with the updated DTO or `204 No Content`; collection tests accept both. Ownership probes expect `404 Not Found` so the API does not disclose whether another customer's ticket exists.

