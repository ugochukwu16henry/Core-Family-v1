export interface CounselorSummary {
  id: string;
  userId: string;
  firstName: string;
  lastName: string;
  bio?: string | null;
  country?: string | null;
  city?: string | null;
  preferredLanguage: string;
  specialization?: string | null;
  hourlyRateUsd: number;
  languages: string[];
  acceptsNewClients: boolean;
  licenseStatus: string;
  averageRating: number;
  reviewCount: number;
}

export interface CounselorMatchRequest {
  challenge: string;
  preferredLanguage?: string | null;
  country?: string | null;
  maxHourlyRateUsd?: number | null;
  durationMinutes?: number;
  top?: number;
}

export interface CounselorMatchResult {
  counselor: CounselorSummary;
  score: number;
  reasons: string[];
}
