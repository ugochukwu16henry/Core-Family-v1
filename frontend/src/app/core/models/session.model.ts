export interface SessionSummary {
  id: string;
  counselorId: string;
  clientId: string;
  counselorName: string;
  clientName: string;
  scheduledAt: string;
  durationMinutes: number;
  status: string;
  amountPaid: number;
  platformCommission: number;
  isPaid: boolean;
  paymentStatus: string;
  notes?: string | null;
  meetingUrl?: string | null;
}

export interface BookSessionRequest {
  counselorId: string;
  scheduledAt: string;
  durationMinutes: number;
  notes?: string | null;
}

export interface RescheduleSessionRequest {
  scheduledAt: string;
  durationMinutes: number;
  notes?: string | null;
}
