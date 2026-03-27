import { CommonModule } from '@angular/common';
import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { Title } from '@angular/platform-browser';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss'
})
export class HomeComponent {
  private readonly title = inject(Title);

  readonly impactStats = [
    { value: '5', label: 'guided content tracks' },
    { value: '1:1', label: 'counseling pathway support' },
    { value: '7', label: 'core user journeys designed' }
  ];

  readonly pillars = [
    {
      title: 'Season-based learning',
      description: 'Singles, couples, parents, and families each get a tailored content arc instead of a generic content dump.'
    },
    {
      title: 'Counseling with continuity',
      description: 'Sessions connect to progress, notes, and follow-up actions so support does not restart every week.'
    },
    {
      title: 'Structured growth dashboard',
      description: 'Your next lesson, enrollment history, and milestones live in one accountable system.'
    }
  ];

  readonly testimonials = [
    {
      quote: 'This feels less like another content site and more like a guided path for real family work.',
      author: 'Early product direction review'
    },
    {
      quote: 'The combination of learning, support, and progress tracking is exactly what counseling products usually miss.',
      author: 'Platform planning session'
    }
  ];

  constructor() {
    this.title.setTitle('Core Family | Counseling and growth platform');
  }
}