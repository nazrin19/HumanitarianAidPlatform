namespace HumanitarianAidPlatform.Application.Admin;

public interface IAdminUserService
{
    Task<IReadOnlyList<PendingAccountResponse>> GetPendingApplicationsAsync(
        CancellationToken cancellationToken,
        string? accountType = null);

    Task<ApplicantDecisionResult> ApproveApplicationAsync(
        Guid userId,
        CancellationToken cancellationToken,
        string? expectedAccountType = null);

    Task<ApplicantDecisionResult> RejectApplicationAsync(
        Guid userId,
        CancellationToken cancellationToken,
        string? expectedAccountType = null);

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
