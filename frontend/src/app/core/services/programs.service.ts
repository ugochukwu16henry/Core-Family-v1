import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { EnrollmentSummary, ProgramDetail, ProgramSummary } from '../models/program.model';

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
}
