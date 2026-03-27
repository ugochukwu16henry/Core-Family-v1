export interface ProgramSummary {
  id: string;
  title: string;
  description: string;
  price: number;
  durationWeeks: number;
  category: string;
  instructorName: string;
  lessonCount: number;
}

export interface LessonSummary {
  id: string;
  title: string;
  orderIndex: number;
  isRequired: boolean;
  contentType?: string;
  isFree: boolean;
  price: number;
}

export interface ProgramDetail {
  id: string;
  title: string;
  description: string;
  price: number;
  durationWeeks: number;
  category: string;
  instructorId: string;
  instructorName: string;
  lessons: LessonSummary[];
}

export interface EnrollmentSummary {
  enrollmentId: string;
  programId: string;
  programTitle: string;
  enrolledAt: string;
  completedAt?: string | null;
  totalLessons: number;
  completedLessons: number;
}
