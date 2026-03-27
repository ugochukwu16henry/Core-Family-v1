import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';

import { ProgramsService } from '../../core/services/programs.service';
import { ProgramLearning } from '../../core/models/program.model';

@Component({
  selector: 'app-learning-program',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './learning-program.component.html',
  styleUrl: './learning-program.component.scss'
})
export class LearningProgramComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly programsService = inject(ProgramsService);

  readonly learning = signal<ProgramLearning | null>(null);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  constructor() {
    const programId = this.route.snapshot.paramMap.get('programId');

    if (!programId) {
      this.error.set('Program not found.');
      this.loading.set(false);
      return;
    }

    this.programsService.getLearningProgram(programId).subscribe({
      next: (learning) => {
        this.learning.set(learning);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(err?.error?.message || 'Unable to load learning program.');
        this.loading.set(false);
      }
    });
  }
}
