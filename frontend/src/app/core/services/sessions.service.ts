import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { BookSessionRequest, RescheduleSessionRequest, SessionSummary } from '../models/session.model';

@Injectable({ providedIn: 'root' })
export class SessionsService {
  private readonly http = inject(HttpClient);

  bookSession(payload: BookSessionRequest): Observable<SessionSummary> {
    return this.http.post<SessionSummary>(`${environment.apiUrl}/sessions`, payload);
  }

  getMySessions(): Observable<SessionSummary[]> {
    return this.http.get<SessionSummary[]>(`${environment.apiUrl}/sessions/me`);
  }

  getSession(sessionId: string): Observable<SessionSummary> {
    return this.http.get<SessionSummary>(`${environment.apiUrl}/sessions/${sessionId}`);
  }

  confirmSession(sessionId: string): Observable<SessionSummary> {
    return this.http.post<SessionSummary>(`${environment.apiUrl}/sessions/${sessionId}/confirm`, {});
  }

  cancelSession(sessionId: string): Observable<SessionSummary> {
    return this.http.post<SessionSummary>(`${environment.apiUrl}/sessions/${sessionId}/cancel`, {});
  }

  rescheduleSession(sessionId: string, payload: RescheduleSessionRequest): Observable<SessionSummary> {
    return this.http.post<SessionSummary>(`${environment.apiUrl}/sessions/${sessionId}/reschedule`, payload);
  }
}
