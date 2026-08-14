import { AuthUser } from './auth-user';

export interface ApiAuthResponse {
  accessToken?: string;
  token?: string;
  jwtToken?: string;
  refreshToken?: string;
  expiresAt?: string;
  refreshTokenExpiresAt?: string;
  user?: Partial<AuthUser>;
}