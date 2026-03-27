import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { PaymentsService } from '../../core/services/payments.service';
import { SessionsService } from '../../core/services/sessions.service';
import { SessionSummary } from '../../core/models/session.model';

@Component({
  selector: 'app-sessions',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './sessions.component.html',
  styleUrl: './sessions.component.scss'
})
export class SessionsComponent {
  private readonly sessionsService = inject(SessionsService);
  private readonly paymentsService = inject(PaymentsService);

  readonly sessions = signal<SessionSummary[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly editingSessionId = signal<string | null>(null);
  readonly actionBusy = signal(false);

  rescheduleForm = {
    scheduledAt: '',
    durationMinutes: 60,
    notes: ''
  };

  constructor() {
    this.loadSessions();
  }

  startReschedule(session: SessionSummary): void {
    this.editingSessionId.set(session.id);
    this.rescheduleForm = {
      scheduledAt: new Date(session.scheduledAt).toISOString().slice(0, 16),
      durationMinutes: session.durationMinutes,
      notes: session.notes ?? ''
    };
  }

  submitReschedule(sessionId: string): void {
    this.actionBusy.set(true);
    this.sessionsService.rescheduleSession(sessionId, {
      scheduledAt: new Date(this.rescheduleForm.scheduledAt).toISOString(),
      durationMinutes: this.rescheduleForm.durationMinutes,
      notes: this.rescheduleForm.notes
    }).subscribe({
      next: (updated) => this.replaceSession(updated),
      error: (err) => {
        this.error.set(err?.error?.message || 'Could not reschedule session.');
        this.actionBusy.set(false);
      }
    });
  }

  cancelSession(sessionId: string): void {
    this.actionBusy.set(true);
    this.sessionsService.cancelSession(sessionId).subscribe({
      next: (updated) => this.replaceSession(updated),
      error: (err) => {
        this.error.set(err?.error?.message || 'Could not cancel session.');
        this.actionBusy.set(false);
      }
    });
  }

  payForSession(sessionId: string): void {
    this.actionBusy.set(true);
    this.paymentsService.createSessionCheckout(sessionId, {
      provider: 'stripe',
      currency: 'USD',
      successUrl: window.location.href,
      cancelUrl: window.location.href
    }).subscribe({
      next: (checkout) => {
        if (checkout.requiresRedirect && checkout.checkoutUrl) {
          window.location.assign(checkout.checkoutUrl);
          return;
        }

        this.loadSessions();
      },
      error: (err) => {
        this.error.set(err?.error?.message || 'Could not start payment.');
        this.actionBusy.set(false);
      }
    });
  }

  private loadSessions(): void {
    this.loading.set(true);
    this.error.set(null);
    this.sessionsService.getMySessions().subscribe({
      next: (sessions) => {
        this.sessions.set(sessions);
        this.loading.set(false);
        this.actionBusy.set(false);
      },
      error: (err) => {
        this.error.set(err?.error?.message || 'Could not load sessions.');
        this.loading.set(false);
        this.actionBusy.set(false);
      }
    });
  }

  private replaceSession(updated: SessionSummary): void {
    this.sessions.set(this.sessions().map((session) => session.id === updated.id ? updated : session));
    this.editingSessionId.set(null);
    this.actionBusy.set(false);
  }
}
