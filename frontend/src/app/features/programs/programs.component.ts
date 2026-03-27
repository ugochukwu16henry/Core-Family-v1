import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { EMPTY, switchMap } from 'rxjs';

import { AuthService } from '../../core/services/auth.service';
import { PaymentsService } from '../../core/services/payments.service';
import { ProgramsService } from '../../core/services/programs.service';
import { ProgramSummary } from '../../core/models/program.model';

@Component({
  selector: 'app-programs',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './programs.component.html',
  styleUrl: './programs.component.scss'
})
export class ProgramsComponent {
  private readonly programsService = inject(ProgramsService);
  private readonly paymentsService = inject(PaymentsService);
  private readonly authService = inject(AuthService);

  readonly programs = signal<ProgramSummary[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly actionInProgress = signal(false);
  readonly enrolledProgramIds = signal<Set<string>>(new Set());
  readonly isAuthenticated = this.authService.isAuthenticated;

  constructor() {
    this.loadPrograms();
  }

  enroll(program: ProgramSummary): void {
    if (!this.isAuthenticated()) {
      this.error.set('Please login to enroll in a program.');
      return;
    }

    this.error.set(null);
    this.actionInProgress.set(true);

    const enroll$ = this.programsService.enroll(program.id);

    if (program.price <= 0) {
      enroll$.subscribe({
        next: () => this.markAsEnrolled(program.id),
        error: (err) => this.setActionError(err),
        complete: () => this.actionInProgress.set(false)
      });
      return;
    }

    this.paymentsService.createProgramCheckout(program.id, {
      provider: 'stripe',
      currency: 'USD',
      successUrl: window.location.href,
      cancelUrl: window.location.href
    }).pipe(
      switchMap((checkout) => {
        if (checkout.requiresRedirect && checkout.checkoutUrl) {
          window.location.assign(checkout.checkoutUrl);
          this.actionInProgress.set(false);
          return EMPTY;
        }

        if (checkout.status !== 'Completed') {
          throw new Error('Payment is still pending. Please try again shortly.');
        }

        return enroll$;
      })
    ).subscribe({
      next: () => this.markAsEnrolled(program.id),
      error: (err) => this.setActionError(err),
      complete: () => this.actionInProgress.set(false)
    });
  }

  private markAsEnrolled(programId: string): void {
    const current = new Set(this.enrolledProgramIds());
    current.add(programId);
    this.enrolledProgramIds.set(current);
    this.actionInProgress.set(false);
  }

  private setActionError(err: unknown): void {
    const message = (err as { error?: { message?: string }; message?: string })?.error?.message
      || (err as { message?: string })?.message
      || 'Unable to enroll right now.';
    this.error.set(message);
    this.actionInProgress.set(false);
  }

  private loadPrograms(): void {
    this.loading.set(true);
    this.error.set(null);

    this.programsService.getPrograms().subscribe({
      next: (programs) => {
        this.programs.set(programs);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Could not load programs. Please try again.');
        this.loading.set(false);
      }
    });
  }
}
