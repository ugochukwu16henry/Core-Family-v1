import { CommonModule } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';

import { ProgramsService } from '../../core/services/programs.service';
import { LessonPlayer } from '../../core/models/program.model';

@Component({
  selector: 'app-lesson-player',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './lesson-player.component.html',
  styleUrl: './lesson-player.component.scss'
})
export class LessonPlayerComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly programsService = inject(ProgramsService);

  readonly lesson = signal<LessonPlayer | null>(null);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);

  private readonly programId = this.route.snapshot.paramMap.get('programId');
  private readonly lessonId = this.route.snapshot.paramMap.get('lessonId');

  constructor() {
    if (!this.programId || !this.lessonId) {
      this.error.set('Lesson route is invalid.');
      this.loading.set(false);
      return;
    }

    this.loadLesson();
  }

  markProgress(secondsToAdd = 60, complete = false): void {
    if (!this.programId || !this.lessonId || !this.lesson()) {
      return;
    }

    this.saving.set(true);
    const nextSeconds = (this.lesson()?.secondsWatched ?? 0) + secondsToAdd;

    this.programsService.updateLessonProgress(this.programId, this.lessonId, {
      secondsWatched: nextSeconds,
      markCompleted: complete
    }).subscribe({
      next: (lesson) => {
        this.lesson.set(lesson);
        this.saving.set(false);
      },
      error: (err) => {
        this.error.set(err?.error?.message || 'Could not save lesson progress.');
        this.saving.set(false);
      }
    });
  }

  private loadLesson(): void {
    if (!this.programId || !this.lessonId) {
      return;
    }

    this.programsService.getLesson(this.programId, this.lessonId).subscribe({
      next: (lesson) => {
        this.lesson.set(lesson);
        this.loading.set(false);
      },
      error: (err) => {
        this.error.set(err?.error?.message || 'Could not load lesson.');
        this.loading.set(false);
      }
    });
  }
}
