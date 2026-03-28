import { Component, effect, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { ProgramsService } from '../../core/services/programs.service';
import { Achievement, LearningStreak, ProgressSummary } from '../../core/models/program.model';

@Component({
  selector: 'app-progress-dashboard',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './progress-dashboard.component.html',
  styleUrl: './progress-dashboard.component.scss'
})
export class ProgressDashboardComponent {
  private readonly programsService = inject(ProgramsService);

  progress = signal<ProgressSummary | null>(null);
  achievements = signal<Achievement[]>([]);
  streak = signal<LearningStreak | null>(null);
  loading = signal(true);
  error = signal<string | null>(null);

  constructor() {
    effect(() => {
      this.loadData();
    });
  }

  private loadData(): void {
    this.loading.set(true);
    this.error.set(null);

    Promise.all([
      this.programsService.getProgressSummary().toPromise(),
      this.programsService.getMyAchievements().toPromise(),
      this.programsService.getMyStreak().toPromise()
    ]).then(([progress, achievements, streak]) => {
      if (progress) this.progress.set(progress);
      if (achievements) this.achievements.set(achievements);
      if (streak) this.streak.set(streak);
      this.loading.set(false);
    }).catch(err => {
      this.error.set(err?.error?.message || 'Failed to load data');
      this.loading.set(false);
    });
  }

  getMilestones(): Array<{ threshold: number; label: string; icon: string }> {
    return [
      { threshold: 1, label: 'First Step', icon: '🎯' },
      { threshold: 3, label: 'Early Achiever', icon: '⭐' },
      { threshold: 5, label: 'Dedicated Learner', icon: '🏆' },
      { threshold: 10, label: 'Knowledge Master', icon: '👑' }
    ];
  }

  getUnlockedMilestones(): Array<{ threshold: number; label: string; icon: string }> {
    const completed = this.progress()?.completedPrograms ?? 0;
    return this.getMilestones().filter(m => m.threshold <= completed);
  }

  getNextMilestone(): { threshold: number; label: string; icon: string } | null {
    const completed = this.progress()?.completedPrograms ?? 0;
    return this.getMilestones().find(m => m.threshold > completed) ?? null;
  }

  getCompletionColor(percentage: number): string {
    if (percentage >= 80) return '#4caf50';
    if (percentage >= 60) return '#8bc34a';
    if (percentage >= 40) return '#ffc107';
    if (percentage >= 20) return '#ff9800';
    return '#f44336';
  }

  getUnlockedAchievements(): Achievement[] {
    return this.achievements().filter(a => a.isUnlocked);
  }

  getLockedAchievements(): Achievement[] {
    return this.achievements()
      .filter(a => !a.isUnlocked)
      .slice(0, 3); // Show next 3 locked achievements
  }

  downloadCertificate(programTitle: string, programId: string): void {
    this.programsService.downloadCertificate(programId).subscribe({
      next: (blob) => {
        const link = document.createElement('a');
        link.href = window.URL.createObjectURL(blob);
        link.download = `${programTitle}-certificate.pdf`;
        link.click();
        window.URL.revokeObjectURL(link.href);
      },
      error: (err) => {
        alert('Note: Certificate PDF generation not yet implemented on backend');
      }
    });
  }
}
