import { allowedStatusTransitions, formatMinutes } from './ticket.models';

describe('ticket business rules', () => {
  it('only permits a customer to close a resolved ticket', () => {
    expect(allowedStatusTransitions('Open', 'Customer')).toEqual([]);
    expect(allowedStatusTransitions('Resolved', 'Customer')).toEqual(['Closed']);
  });

  it('prevents every role from reopening a closed ticket', () => {
    expect(allowedStatusTransitions('Closed', 'Admin')).toEqual([]);
    expect(allowedStatusTransitions('Closed', 'SupportAgent')).toEqual([]);
  });

  it('keeps agent and admin transitions aligned with the API state machine', () => {
    expect(allowedStatusTransitions('Open', 'SupportAgent')).toEqual(['InProgress']);
    expect(allowedStatusTransitions('InProgress', 'Admin')).toEqual(['Resolved']);
    expect(allowedStatusTransitions('Resolved', 'SupportAgent')).toEqual(['InProgress', 'Closed']);
  });

  it('formats logged minutes for display', () => {
    expect(formatMinutes(135)).toBe('2h 15m');
  });
});
