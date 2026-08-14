import { UserRole } from './user-role';

export interface ManagedUser {
  id: string;
  firstName: string;
  lastName: string;
  email: string;
  role: UserRole;
  isActive: boolean;
  createdAt?: string;
  assignedTicketCount?: number;
}

