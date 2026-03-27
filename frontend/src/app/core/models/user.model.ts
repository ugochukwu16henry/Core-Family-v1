export type UserRole = 'Client' | 'Instructor' | 'Counselor' | 'Admin' | 'Moderator';
export type UserCategory = 'Single' | 'MarriedMan' | 'MarriedWoman' | 'Parent' | 'Family' | 'Youth';

export interface UserSummary {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  role: UserRole;
  category: UserCategory;
  avatarUrl?: string | null;
}

export interface UpdateProfileRequest {
  firstName?: string | null;
  lastName?: string | null;
  avatarUrl?: string | null;
  bio?: string | null;
  phoneNumber?: string | null;
  country?: string | null;
}export interface User {
}
