# Support Management Angular client

Angular 21 standalone client for the Support Ticket Management System. It uses Angular Material, Reactive Forms, RxJS, lazy feature routes, role guards, and a JWT HTTP interceptor.

From this directory:

```bash
npm ci
npm start
npm test -- --watch=false
npm run build
```

Development requests target `http://localhost:5052/api`; production builds use the relative `/api` path expected by the included Nginx configuration. See the [root README](../../README.md) for database/API setup, seeded accounts, architecture, and the complete verification workflow.
