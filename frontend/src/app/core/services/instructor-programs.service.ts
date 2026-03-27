import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { InstructorProgramSummary, InstructorProgramUpsertRequest } from '../models/instructor-program.model';

@Injectable({ providedIn: 'root' })
export class InstructorProgramsService {
  private readonly http = inject(HttpClient);

  getMyPrograms(): Observable<InstructorProgramSummary[]> {
    return this.http.get<InstructorProgramSummary[]>(`${environment.apiUrl}/instructorprograms/me`);
  }

  createProgram(payload: InstructorProgramUpsertRequest): Observable<InstructorProgramSummary> {
    return this.http.post<InstructorProgramSummary>(`${environment.apiUrl}/instructorprograms`, payload);
  }

  publishProgram(programId: string): Observable<InstructorProgramSummary> {
    return this.http.post<InstructorProgramSummary>(`${environment.apiUrl}/instructorprograms/${programId}/publish`, {});
  }
}
