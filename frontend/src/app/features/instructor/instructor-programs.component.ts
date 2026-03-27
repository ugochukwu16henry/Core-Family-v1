import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';

import { AuthService } from '../../core/services/auth.service';
import { InstructorProgramsService } from '../../core/services/instructor-programs.service';
import { ContentCategory, InstructorProgramSummary } from '../../core/models/instructor-program.model';

@Component({
  selector: 'app-instructor-programs',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './instructor-programs.component.html',
  styleUrl: './instructor-programs.component.scss'
})
export class InstructorProgramsComponent {
  private readonly authService = inject(AuthService);
  private readonly instructorPrograms = inject(InstructorProgramsService);

  readonly user = this.authService.currentUser;
  readonly programs = signal<InstructorProgramSummary[]>([]);
  readonly loading = signal(true);
  readonly submitting = signal(false);
  readonly error = signal<string | null>(null);

  readonly categories: ContentCategory[] = [
    'Married',
    'Singles',
    'Parenting',
    'FamilyFinance',
    'ConflictResolution',
    'General'
  ];

  form = {
    title: '',
    description: '',
    price: 3,
    durationWeeks: 4,
    category: 'General' as ContentCategory
  };

  constructor() {
    this.loadPrograms();
  }

  get isInstructor(): boolean {
    return this.user()?.role === 'Instructor';
  }

  createProgram(): void {
    if (!this.isInstructor) {
      this.error.set('Only instructor accounts can create programs.');
      return;
    }

    this.submitting.set(true);
    this.error.set(null);

    this.instructorPrograms.createProgram(this.form).subscribe({
      next: (created) => {
        this.programs.set([created, ...this.programs()]);
        this.form = {
          title: '',
          description: '',
          price: 3,
          durationWeeks: 4,
          category: 'General'
        };
        this.submitting.set(false);
      },
      error: (err) => {
        this.error.set(err?.error?.message || 'Could not create program.');
        this.submitting.set(false);
      }
    });
  }

  publishProgram(programId: string): void {
    this.instructorPrograms.publishProgram(programId).subscribe({
      next: (updated) => {
        this.programs.set(this.programs().map((p) => (p.id === programId ? updated : p)));
      },
      error: (err) => {
        this.error.set(err?.error?.message || 'Could not publish program.');
      }
    });
  }

  private loadPrograms(): void {
    this.loading.set(true);
    this.error.set(null);

    this.instructorPrograms.getMyPrograms().subscribe({
      next: (programs) => {
        this.programs.set(programs);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(err?.error?.message || 'Could not load instructor programs.');
        this.loading.set(false);
      }
    });
  }
}
