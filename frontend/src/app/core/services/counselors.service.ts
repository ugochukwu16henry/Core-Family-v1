import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { CounselorMatchRequest, CounselorMatchResult, CounselorSummary } from '../models/counselor.model';

@Injectable({ providedIn: 'root' })
export class CounselorsService {
  private readonly http = inject(HttpClient);

  searchCounselors(query: {
    language?: string;
    specialization?: string;
    country?: string;
    acceptsNewClients?: boolean;
  }): Observable<CounselorSummary[]> {
    let params = new HttpParams();

    if (query.language) {
      params = params.set('language', query.language);
    }

    if (query.specialization) {
      params = params.set('specialization', query.specialization);
    }

    if (query.country) {
      params = params.set('country', query.country);
    }

    if (query.acceptsNewClients !== undefined) {
      params = params.set('acceptsNewClients', query.acceptsNewClients);
    }

    return this.http.get<CounselorSummary[]>(`${environment.apiUrl}/counselors`, { params });
  }

  matchCounselors(payload: CounselorMatchRequest): Observable<CounselorMatchResult[]> {
    return this.http.post<CounselorMatchResult[]>(`${environment.apiUrl}/counselors/match`, payload);
  }
}
