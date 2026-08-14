import { UserRole } from './user-role';

export function normalizeRole(value: unknown): UserRole {
  const role = String(Array.isArray(value) ? value[0] : (value ?? ''))
    .replace(/[\s_-]/g, '')
    .toLowerCase();

  if (role === 'admin') return 'Admin';
  if (role === 'supportagent' || role === 'agent') return 'SupportAgent';
  return 'Customer';
}

