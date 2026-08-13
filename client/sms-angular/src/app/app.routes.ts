import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { roleGuard } from './core/guards/role.guard';

export const routes: Routes = [
  {
    path: 'login',
    loadComponent: () =>
      import('./features/auth/login.component').then((module) => module.LoginComponent),
  },
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/layout/app-shell.component').then((module) => module.AppShellComponent),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'tickets' },
      {
        path: 'dashboard',
        canActivate: [roleGuard],
        data: { roles: ['Admin'] },
        loadComponent: () =>
          import('./features/dashboard/dashboard.component').then(
            (module) => module.DashboardComponent,
          ),
      },
      {
        path: 'tickets',
        loadComponent: () =>
          import('./features/tickets/ticket-list.component').then(
            (module) => module.TicketListComponent,
          ),
      },
      {
        path: 'tickets/new',
        canActivate: [roleGuard],
        data: { roles: ['Customer'] },
        loadComponent: () =>
          import('./features/tickets/ticket-create.component').then(
            (module) => module.TicketCreateComponent,
          ),
      },
      {
        path: 'tickets/:number',
        loadComponent: () =>
          import('./features/tickets/ticket-detail.component').then(
            (module) => module.TicketDetailComponent,
          ),
      },
      {
        path: 'users',
        canActivate: [roleGuard],
        data: { roles: ['Admin'] },
        loadComponent: () =>
          import('./features/users/user-management.component').then(
            (module) => module.UserManagementComponent,
          ),
      },
    ],
  },
  {
    path: '**',
    loadComponent: () =>
      import('./features/errors/not-found.component').then((module) => module.NotFoundComponent),
  },
];
