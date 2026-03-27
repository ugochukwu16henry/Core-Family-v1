import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { ProgramsService } from '../../core/services/programs.service';
import { EnrollmentSummary } from '../../core/models/program.model';

@Component({
  selector: 'app-my-learning',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './my-learning.component.html',
  styleUrl: './my-learning.component.scss'
})
export class MyLearningComponent {
  private readonly programsService = inject(ProgramsService);

  readonly enrollments = signal<EnrollmentSummary[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  constructor() {
    this.loadEnrollments();
  }

  private loadEnrollments(): void {
    this.loading.set(true);
    this.error.set(null);

    this.programsService.getMyEnrollments().subscribe({
      next: (enrollments) => {
        this.enrollments.set(enrollments);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Could not load your enrollments right now.');
        this.loading.set(false);
      }
    });
  }
}
