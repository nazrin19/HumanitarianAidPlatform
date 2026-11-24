namespace HumanitarianAidPlatform.Application.Admin;

public interface IAdminUserService
{
    Task<IReadOnlyList<PendingAccountResponse>> GetPendingApplicationsAsync(
        CancellationToken cancellationToken);

    Task<ApplicantDecisionResult> ApproveApplicationAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<ApplicantDecisionResult> RejectApplicationAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<AdminAccountResponse?> GetAccountAsync(
        Guid userId,
        CancellationToken cancellationToken);

    Task<StaffAccountCreationResult> CreateStaffAccountAsync(
        CreateStaffAccountRequest request,
        CancellationToken cancellationToken);

    Task<bool> ResetActiveAdminPasswordAsync(
        string email,
        string newPassword,
        CancellationToken cancellationToken);

    Task<bool> BootstrapFirstAdminAsync(
        string? email,
        string? password,
        CancellationToken cancellationToken);
}
