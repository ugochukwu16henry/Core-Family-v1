import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { AuthService } from '../../core/services/auth.service';
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
  private readonly authService = inject(AuthService);

  readonly programs = signal<ProgramSummary[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly enrolledProgramIds = signal<Set<string>>(new Set());
  readonly isAuthenticated = this.authService.isAuthenticated;

  constructor() {
    this.loadPrograms();
  }

  enroll(programId: string): void {
    if (!this.isAuthenticated()) {
      this.error.set('Please login to enroll in a program.');
      return;
    }

    this.programsService.enroll(programId).subscribe({
      next: () => {
        const current = new Set(this.enrolledProgramIds());
        current.add(programId);
        this.enrolledProgramIds.set(current);
      },
      error: (err) => {
        const message = err?.error?.message || 'Unable to enroll right now.';
        this.error.set(message);
      }
    });
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
