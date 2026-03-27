import { Routes } from '@angular/router';

import { authGuard } from './core/guards/auth.guard';
import { guestGuard } from './core/guards/guest.guard';
import { AppShellComponent } from './layout/app-shell.component';
import { AuthShellComponent } from './layout/auth-shell.component';

export const routes: Routes = [
	{
		path: '',
		component: AppShellComponent,
		children: [
			{
				path: '',
				loadComponent: () => import('./features/home/home.component').then((m) => m.HomeComponent)
			},
			{
				path: 'programs',
				loadComponent: () => import('./features/programs/programs.component').then((m) => m.ProgramsComponent)
			},
			{
				path: 'programs/:programId/learn',
				canActivate: [authGuard],
				loadComponent: () => import('./features/programs/learning-program.component').then((m) => m.LearningProgramComponent)
			},
			{
				path: 'programs/:programId/lessons/:lessonId',
				canActivate: [authGuard],
				loadComponent: () => import('./features/programs/lesson-player.component').then((m) => m.LessonPlayerComponent)
			},
			{
				path: 'dashboard',
				canActivate: [authGuard],
				loadComponent: () => import('./features/home/dashboard.component').then((m) => m.DashboardComponent)
			},
			{
				path: 'sessions',
				canActivate: [authGuard],
				loadComponent: () => import('./features/sessions/sessions.component').then((m) => m.SessionsComponent)
			},
			{
				path: 'my-learning',
				canActivate: [authGuard],
				loadComponent: () => import('./features/programs/my-learning.component').then((m) => m.MyLearningComponent)
			},
			{
				path: 'billing',
				canActivate: [authGuard],
				loadComponent: () => import('./features/payments/billing.component').then((m) => m.BillingComponent)
			},
			{
				path: 'instructor/programs',
				canActivate: [authGuard],
				loadComponent: () => import('./features/instructor/instructor-programs.component').then((m) => m.InstructorProgramsComponent)
			}
		]
	},
	{
		path: 'auth',
		component: AuthShellComponent,
		canActivate: [guestGuard],
		children: [
			{
				path: 'login',
				loadComponent: () => import('./features/auth/login.component').then((m) => m.LoginComponent)
			},
			{
				path: 'register',
				loadComponent: () => import('./features/auth/register.component').then((m) => m.RegisterComponent)
			},
			{
				path: '',
				pathMatch: 'full',
				redirectTo: 'login'
			}
		]
	},
	{
		path: '**',
		redirectTo: ''
	}
];
