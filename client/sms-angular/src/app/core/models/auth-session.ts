import { AuthUser } from './auth-user';

export interface AuthSession {
  accessToken: string;
  refreshToken: string;
  expiresAt?: string;
  refreshTokenExpiresAt?: string;
  user: AuthUser;
}