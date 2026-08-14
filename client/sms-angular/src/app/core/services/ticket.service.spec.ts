import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { environment } from '../../../environments/environment';
import { TicketService } from './ticket.service';

describe('TicketService', () => {
  let service: TicketService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(TicketService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('sends server-side paging, filters, search, and sorting', () => {
    let count = -1;
    service
      .getTickets({
        page: 2,
        pageSize: 10,
        search: 'printer',
        status: 'Open',
        priority: 'High',
        sortBy: 'priority',
        sortDirection: 'asc',
      })
      .subscribe((result) => (count = result.totalCount));

    const request = http.expectOne(
      (candidate) => candidate.url === `${environment.apiUrl}/tickets`,
    );
    expect(request.request.params.get('page')).toBe('2');
    expect(request.request.params.get('status')).toBe('Open');
    expect(request.request.params.get('search')).toBe('printer');
    expect(request.request.params.get('sortDirection')).toBe('asc');
    request.flush({ items: [], page: 2, pageSize: 10, totalCount: 24, totalPages: 3 });
    expect(count).toBe(24);
  });

  it('normalizes a ticket timeline without exposing transport quirks to components', () => {
    service.getTicket(16).subscribe((ticket) => {
      expect(ticket.number).toBe(16);
      expect(ticket.activities[0].type).toBe('StatusChanged');
      expect(ticket.totalTimeMinutes).toBe(45);
    });

    const request = http.expectOne(`${environment.apiUrl}/tickets/16`);
    request.flush({
      ticketNumber: 16,
      title: 'Printer issue',
      status: 'InProgress',
      priority: 'High',
      createdAt: '2026-01-01',
      totalTimeMinutes: 45,
      timeline: [
        {
          id: 'activity-1',
          activityType: 'StatusChanged',
          description: 'Status changed',
          createdAt: '2026-01-01',
        },
      ],
    });
  });
});
