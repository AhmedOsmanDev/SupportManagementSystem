# Demonstration script

This five-to-seven-minute path covers the assessment's highest-value behavior. Start with a clean seeded database so the ticket counts are predictable.

## Before recording

1. Start the API and Angular application using either the Docker Compose or local setup in the root README.
2. Open the browser developer tools on the **Network** tab; keep passwords and JWTs out of the recording.
3. Have the seeded admin, agent, and two customer accounts ready.
4. Verify Swagger is reachable at `http://localhost:5052/swagger` and the application at `http://localhost:4200`.

## Recording flow

1. **Customer creates a ticket**
   - Sign in as the first customer.
   - Create a high-priority ticket with a clear title and description.
   - Show the generated ticket number and that the ticket appears in the customer's list.

2. **Customer isolation**
   - Sign out and sign in as the second customer.
   - Show that the new ticket is absent from the list.
   - Optionally use Swagger/Postman to request the known ticket number and show the safe `404 Not Found` response.

3. **Admin triage and dashboard**
   - Sign in as admin.
   - Show dashboard totals, the chart, critical/open metrics, average resolution time, and agent workload.
   - Filter/search/sort the ticket list, then assign the new ticket to the seeded agent and change its priority.

4. **Agent work**
   - Sign in as the assigned support agent.
   - Show the assigned-ticket view.
   - Move the ticket to In Progress, add a comment, log time, then resolve it.
   - Open the timeline to show assignment, priority, status, and comment activity; show the calculated total time.

5. **Customer closes the resolved ticket**
   - Sign in as the first customer.
   - Add a reply and close the resolved ticket.
   - Show that the timeline and final Closed status are visible.

6. **API and tests**
   - Briefly show Swagger's JWT authorization support and the supplied Postman collection.
   - Show the backend and frontend test commands completing successfully.

## Capture checklist

- Keep the browser window at least 1280×720.
- Avoid showing `.env`, `appsettings.Development.json`, Authorization headers, or access tokens.
- Use synthetic customer data only.
- Capture the dashboard, ticket list, ticket details/timeline, create form, and Swagger page if screenshots are submitted instead of video.

