export type ContentCategory =
  | 'Married'
  | 'Singles'
  | 'Parenting'
  | 'FamilyFinance'
  | 'ConflictResolution'
  | 'General';

export interface InstructorProgramUpsertRequest {
  title: string;
  description: string;
  price: number;
  durationWeeks: number;
  category: ContentCategory;
}

export interface InstructorProgramSummary {
  id: string;
  title: string;
  description: string;
  price: number;
  durationWeeks: number;
  category: ContentCategory;
  isPublished: boolean;
  lessonCount: number;
  updatedAt: string;
}
