# Screenshot deliverable

The checked-in PNG files were captured from the seeded local application with synthetic data. To refresh them after a UI change, start the API and client, then run `scripts/capture-screenshots.ps1` from the repository root. The expected files are:

- `01-login.png`
- `02-customer-ticket-list.png`
- `03-create-ticket.png`
- `04-ticket-timeline-and-time.png`
- `05-admin-dashboard.png`
- `06-agent-workload.png`
- `07-swagger.png`

Do not include JWTs, connection strings, passwords, real customer details, browser password-manager popups, or other secrets. A short video can alternatively follow [`docs/demo-script.md`](../demo-script.md).
