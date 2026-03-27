import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { EnrollmentSummary, LessonPlayer, ProgramDetail, ProgramLearning, ProgramSummary, UpdateLessonProgressRequest } from '../models/program.model';

@Injectable({ providedIn: 'root' })
export class ProgramsService {
  private readonly http = inject(HttpClient);

  getPrograms(): Observable<ProgramSummary[]> {
    return this.http.get<ProgramSummary[]>(`${environment.apiUrl}/programs`);
  }

  getProgramById(id: string): Observable<ProgramDetail> {
    return this.http.get<ProgramDetail>(`${environment.apiUrl}/programs/${id}`);
  }

  enroll(programId: string): Observable<EnrollmentSummary> {
    return this.http.post<EnrollmentSummary>(`${environment.apiUrl}/programs/${programId}/enroll`, {});
  }

  getMyEnrollments(): Observable<EnrollmentSummary[]> {
    return this.http.get<EnrollmentSummary[]>(`${environment.apiUrl}/programs/me/enrollments`);
  }

  getLearningProgram(programId: string): Observable<ProgramLearning> {
    return this.http.get<ProgramLearning>(`${environment.apiUrl}/programs/${programId}/learn`);
  }

  getLesson(programId: string, lessonId: string): Observable<LessonPlayer> {
    return this.http.get<LessonPlayer>(`${environment.apiUrl}/programs/${programId}/lessons/${lessonId}`);
  }

  updateLessonProgress(programId: string, lessonId: string, payload: UpdateLessonProgressRequest): Observable<LessonPlayer> {
    return this.http.post<LessonPlayer>(`${environment.apiUrl}/programs/${programId}/lessons/${lessonId}/progress`, payload);
  }
}
