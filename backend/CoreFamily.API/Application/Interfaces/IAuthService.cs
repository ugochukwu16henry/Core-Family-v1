using CoreFamily.API.Application.DTOs;

namespace CoreFamily.API.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto> RegisterAsync(RegisterDto dto);
    Task<AuthResponseDto> LoginAsync(LoginDto dto);
    Task<AuthResponseDto> RefreshTokenAsync(string refreshToken);
    Task RevokeTokenAsync(string refreshToken);
    Task ForgotPasswordAsync(string email);
    Task ResetPasswordAsync(ResetPasswordDto dto);
    Task VerifyEmailAsync(string token);
}

public interface IUserService
{
    Task<UserSummaryDto?> GetByIdAsync(Guid userId);
    Task<UserSummaryDto> UpdateProfileAsync(Guid userId, UpdateProfileDto dto);
}

public interface ICounselorService
{
    Task<IReadOnlyList<CounselorSummaryDto>> SearchAsync(CounselorSearchDto search);
    Task<IReadOnlyList<CounselorMatchResultDto>> GetMatchesAsync(Guid userId, CounselorMatchRequestDto request);
    Task<CounselorSummaryDto?> GetByIdAsync(Guid counselorId);
    Task<CounselorSummaryDto> UpsertMyProfileAsync(Guid userId, CounselorProfileUpsertDto dto);
    Task<IReadOnlyList<SessionSummaryDto>> GetCounselorSessionsAsync(Guid userId);
    Task<IReadOnlyList<SessionSummaryDto>> GetClientSessionsAsync(Guid userId);
    Task<SessionSummaryDto?> GetSessionByIdAsync(Guid userId, Guid sessionId);
    Task<SessionSummaryDto> BookSessionAsync(Guid clientUserId, BookSessionDto dto);
    Task<SessionSummaryDto> ConfirmSessionAsync(Guid counselorUserId, Guid sessionId);
    Task<SessionSummaryDto> CancelSessionAsync(Guid userId, Guid sessionId);
    Task<SessionSummaryDto> RescheduleSessionAsync(Guid userId, Guid sessionId, RescheduleSessionDto dto);
}

public interface IProgramService
{
    Task<IReadOnlyList<ProgramSummaryDto>> GetPublishedProgramsAsync(ProgramSearchDto search);
    Task<ProgramDetailDto?> GetPublishedProgramByIdAsync(Guid programId);
    Task<EnrollmentSummaryDto> EnrollAsync(Guid userId, Guid programId);
    Task<IReadOnlyList<EnrollmentSummaryDto>> GetMyEnrollmentsAsync(Guid userId);
    Task<ProgramLearningDto> GetLearningProgramAsync(Guid userId, Guid programId);
    Task<LessonPlayerDto> GetLessonAsync(Guid userId, Guid programId, Guid lessonId);
    Task<LessonPlayerDto> UpdateLessonProgressAsync(Guid userId, Guid programId, Guid lessonId, UpdateLessonProgressDto dto);
    Task<IReadOnlyList<InstructorProgramSummaryDto>> GetMyProgramsAsync(Guid instructorUserId);
    Task<InstructorProgramSummaryDto> CreateProgramAsync(Guid instructorUserId, InstructorProgramUpsertDto dto);
    Task<InstructorProgramSummaryDto> UpdateProgramAsync(Guid instructorUserId, Guid programId, InstructorProgramUpsertDto dto);
    Task<InstructorProgramSummaryDto> PublishProgramAsync(Guid instructorUserId, Guid programId);
    Task<InstructorLessonSummaryDto> AddLessonAsync(Guid instructorUserId, Guid programId, InstructorLessonUpsertDto dto);
    Task<InstructorLessonSummaryDto> UpdateLessonAsync(Guid instructorUserId, Guid programId, Guid lessonId, InstructorLessonUpsertDto dto);
}

public interface IPaymentService
{
    Task<CheckoutSessionDto> CreateProgramCheckoutAsync(Guid userId, Guid programId, CreateCheckoutRequestDto request);
    Task<CheckoutSessionDto> CreateSessionCheckoutAsync(Guid userId, Guid sessionId, CreateCheckoutRequestDto request);
    Task HandleWebhookAsync(string provider, PaymentWebhookDto payload, string rawPayload, string? signature);
    Task<IReadOnlyList<TransactionSummaryDto>> GetMyTransactionsAsync(Guid userId);
    Task<bool> HasCompletedProgramPaymentAsync(Guid userId, Guid programId);
    Task<bool> HasCompletedSessionPaymentAsync(Guid userId, Guid sessionId);
    Task<TransactionSummaryDto> RequestRefundAsync(Guid userId, Guid transactionId, string reason);
}

public interface IAdminService
{
    Task<IReadOnlyList<AdminUserSummaryDto>> GetUsersAsync();
    Task<AdminUserSummaryDto> SetUserActiveStatusAsync(Guid userId, bool isActive);
    Task<IReadOnlyList<AdminReviewSummaryDto>> GetFlaggedReviewsAsync();
    Task<AdminReviewSummaryDto> SetReviewFlagStatusAsync(Guid reviewId, bool isFlagged);
    Task<IReadOnlyList<AdminTransactionSummaryDto>> GetTransactionsAsync();
    Task<AdminTransactionSummaryDto> RefundTransactionAsync(Guid transactionId, string reason);
}

public interface ITokenService
{
    string GenerateAccessToken(Guid userId, string email, IEnumerable<string> roles);
    string GenerateRefreshToken();
    Guid? ValidateRefreshToken(string refreshToken);
}

public interface IPasswordService
{
    string Hash(string password);
    bool Verify(string password, string hash);
}
