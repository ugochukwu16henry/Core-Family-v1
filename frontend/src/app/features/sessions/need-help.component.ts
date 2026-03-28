import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';

import { CounselorMatchResult } from '../../core/models/counselor.model';
import { AuthService } from '../../core/services/auth.service';
import { CounselorsService } from '../../core/services/counselors.service';
import { SessionsService } from '../../core/services/sessions.service';

@Component({
  selector: 'app-need-help',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './need-help.component.html',
  styleUrl: './need-help.component.scss'
})
export class NeedHelpComponent {
  private readonly counselorsService = inject(CounselorsService);
  private readonly sessionsService = inject(SessionsService);
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);

  readonly matches = signal<CounselorMatchResult[]>([]);
  readonly loading = signal(false);
  readonly booking = signal(false);
  readonly error = signal<string | null>(null);
  readonly success = signal<string | null>(null);

  form = {
    challenge: '',
    preferredLanguage: this.authService.currentUser()?.category === 'Youth' ? 'en' : '',
    country: '',
    maxHourlyRateUsd: 80,
    durationMinutes: 60,
    top: 5,
    scheduledAt: ''
  };

  findMatches(): void {
    if (!this.form.challenge.trim()) {
      this.error.set('Please describe the challenge you need help with.');
      return;
    }

    this.loading.set(true);
    this.error.set(null);
    this.success.set(null);

    this.counselorsService.matchCounselors({
      challenge: this.form.challenge.trim(),
      preferredLanguage: this.form.preferredLanguage || null,
      country: this.form.country || null,
      maxHourlyRateUsd: this.form.maxHourlyRateUsd > 0 ? this.form.maxHourlyRateUsd : null,
      durationMinutes: this.form.durationMinutes,
      top: this.form.top
    }).subscribe({
      next: (results) => {
        this.matches.set(results);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(err?.error?.message || 'Could not find counselor matches right now.');
        this.loading.set(false);
      }
    });
  }

  bookWithCounselor(counselorId: string): void {
    if (!this.form.scheduledAt) {
      this.error.set('Select a date and time before booking.');
      return;
    }

    this.booking.set(true);
    this.error.set(null);
    this.success.set(null);

    this.sessionsService.bookSession({
      counselorId,
      scheduledAt: new Date(this.form.scheduledAt).toISOString(),
      durationMinutes: this.form.durationMinutes,
      notes: this.form.challenge.trim()
    }).subscribe({
      next: (session) => {
        this.success.set(`Session booked with ${session.counselorName}. Continue in Sessions to pay and manage details.`);
        this.booking.set(false);
      },
      error: (err) => {
        this.error.set(err?.error?.message || 'Could not book this counselor.');
        this.booking.set(false);
      }
    });
  }

  openSessions(): void {
    void this.router.navigate(['/sessions']);
  }
}
