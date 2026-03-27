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

export interface LessonPlayer {
  lessonId: string;
  programId: string;
  contentId: string;
  title: string;
  orderIndex: number;
  isRequired: boolean;
  contentTitle?: string | null;
  contentDescription?: string | null;
  contentBody?: string | null;
  contentType: string;
  isFree: boolean;
  price: number;
  secondsWatched: number;
  completedAt?: string | null;
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

export interface ProgramLearning {
  programId: string;
  programTitle: string;
  enrolledAt: string;
  completedAt?: string | null;
  totalLessons: number;
  completedLessons: number;
  lessons: LessonPlayer[];
}

export interface UpdateLessonProgressRequest {
  secondsWatched: number;
  markCompleted: boolean;
}
