import { ChangeDetectionStrategy, Component } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { RouterLink } from '@angular/router';

@Component({
  selector: 'app-not-found',
  imports: [RouterLink, MatButtonModule],
  template: `
    <main class="center-state">
      <span class="state-code">404</span>
      <h1>That page isn’t here</h1>
      <p>The address may have changed, or you may not have access.</p>
      <a mat-flat-button color="primary" routerLink="/tickets">Back to tickets</a>
    </main>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class NotFoundComponent {}
