import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { CheckoutSession, CreateCheckoutRequest, TransactionSummary } from '../models/payment.model';

@Injectable({ providedIn: 'root' })
export class PaymentsService {
  private readonly http = inject(HttpClient);

  createProgramCheckout(programId: string, payload: CreateCheckoutRequest): Observable<CheckoutSession> {
    return this.http.post<CheckoutSession>(`${environment.apiUrl}/payments/checkout/program/${programId}`, payload);
  }

  createSessionCheckout(sessionId: string, payload: CreateCheckoutRequest): Observable<CheckoutSession> {
    return this.http.post<CheckoutSession>(`${environment.apiUrl}/payments/checkout/session/${sessionId}`, payload);
  }

  getMyTransactions(): Observable<TransactionSummary[]> {
    return this.http.get<TransactionSummary[]>(`${environment.apiUrl}/payments/me`);
  }
}
