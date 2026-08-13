export type UserRole = 'Admin' | 'SupportAgent' | 'Customer';

export interface AuthUser {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  role: UserRole;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface AuthSession {
  accessToken: string;
  refreshToken?: string;
  expiresAt?: string;
  user: AuthUser;
}

export interface ApiAuthResponse {
  accessToken?: string;
  token?: string;
  jwtToken?: string;
  refreshToken?: string;
  expiresAt?: string;
  user?: Partial<AuthUser>;
}

export function normalizeRole(value: unknown): UserRole {
  const role = String(Array.isArray(value) ? value[0] : (value ?? ''))
    .replace(/[\s_-]/g, '')
    .toLowerCase();

  if (role === 'admin') return 'Admin';
  if (role === 'supportagent' || role === 'agent') return 'SupportAgent';
  return 'Customer';
}
