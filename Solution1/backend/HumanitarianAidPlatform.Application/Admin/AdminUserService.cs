using System.ComponentModel.DataAnnotations;
using System.Text;
using HumanitarianAidPlatform.Application.Auth;
using HumanitarianAidPlatform.Domain.Entities.Users;

namespace HumanitarianAidPlatform.Application.Admin;

public sealed class AdminUserService : IAdminUserService
{
    private static readonly HashSet<string> StaffRoles = new(StringComparer.Ordinal)
    {
        RoleNames.Coordinator,
        RoleNames.CaseManager,
        RoleNames.ResourceManager,
        RoleNames.ShelterAdmin,
        RoleNames.VolunteerCoordinator,
        RoleNames.DonorOfficer,
        RoleNames.PartnerOfficer,
        RoleNames.Admin
    };

    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;

    public AdminUserService(IUserRepository users, IPasswordHasher hasher)
    {
        _users = users;
        _hasher = hasher;
    }

    public async Task<IReadOnlyList<PendingAccountResponse>> GetPendingApplicationsAsync(
        CancellationToken cancellationToken)
    {
        var users = await _users.GetPendingApplicantsAsync(cancellationToken);
        return users.Select(user => new PendingAccountResponse(
            user.Id,
            user.FullName,
            user.Email,
            user.Role.Name,
            user.VolunteerProfile?.Phone ?? user.DonorProfile?.Phone,
            user.VolunteerProfile?.Skills,
            user.DonorProfile?.Type,
            new DateTimeOffset(DateTime.SpecifyKind(user.CreatedAt, DateTimeKind.Utc)))).ToList();
    }

    public Task<ApplicantDecisionResult> ApproveApplicationAsync(
        Guid userId,
        CancellationToken cancellationToken) =>
        DecideOnApplicationAsync(userId, AccountStatus.Active, cancellationToken);

    public Task<ApplicantDecisionResult> RejectApplicationAsync(
        Guid userId,
        CancellationToken cancellationToken) =>
        DecideOnApplicationAsync(userId, AccountStatus.Rejected, cancellationToken);

    private async Task<ApplicantDecisionResult> DecideOnApplicationAsync(
        Guid userId,
        AccountStatus decision,
        CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdAsync(userId, cancellationToken);
        if (user is null ||
            (user.Role.Name != RoleNames.Volunteer && user.Role.Name != RoleNames.Donor))
        {
            return ApplicantDecisionResult.NotFound;
        }

        if (user.AccountStatus != AccountStatus.Pending)
            return ApplicantDecisionResult.NotPending;

        user.AccountStatus = decision;
        if (user.VolunteerProfile is not null)
            user.VolunteerProfile.Availability =
                decision == AccountStatus.Active ? "Available" : "Inactive";
        if (user.DonorProfile is not null)
            user.DonorProfile.IsActive = decision == AccountStatus.Active;

        await _users.UpdateAsync(user, cancellationToken);
        return ApplicantDecisionResult.Updated;
    }

    public async Task<AdminAccountResponse?> GetAccountAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        var user = await _users.GetByIdAsync(userId, cancellationToken);
        return user is null ? null : ToResponse(user);
    }

    public async Task<StaffAccountCreationResult> CreateStaffAccountAsync(
        CreateStaffAccountRequest request,
        CancellationToken cancellationToken)
    {
        if (!StaffRoles.Contains(request.Role))
            return StaffAccountCreationResult.Fail(StaffAccountCreationError.InvalidRole);

        var email = request.Email.Trim().ToLowerInvariant();
        if (await _users.EmailExistsAsync(email, cancellationToken))
            return StaffAccountCreationResult.Fail(
                StaffAccountCreationError.EmailAlreadyExists);

        var role = await _users.GetRoleByNameAsync(request.Role, cancellationToken);
        if (role is null)
            throw new InvalidOperationException(
                $"Configured role '{request.Role}' does not exist in the database.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = _hasher.Hash(request.Password),
            FullName = request.FullName.Trim(),
            RoleId = role.Id,
            AccountStatus = AccountStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        await _users.AddAsync(user, cancellationToken);

        return StaffAccountCreationResult.Ok(ToResponse(user, role.Name));
    }

    public async Task<bool> ResetActiveAdminPasswordAsync(
        string email,
        string newPassword,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(newPassword) ||
            newPassword.Length < 12 ||
            Encoding.UTF8.GetByteCount(newPassword) > 72)
        {
            throw new ArgumentException(
                "The new password must be at least 12 characters and no more than 72 UTF-8 bytes.",
                nameof(newPassword));
        }

        var user = await _users.GetByEmailAsync(
            email.Trim().ToLowerInvariant(),
            cancellationToken);
        if (user is null ||
            user.Role.Name != RoleNames.Admin ||
            user.AccountStatus != AccountStatus.Active)
        {
            return false;
        }

        user.PasswordHash = _hasher.Hash(newPassword);
        await _users.UpdateAsync(user, cancellationToken);
        return true;
    }

    public async Task<bool> BootstrapFirstAdminAsync(
        string? email,
        string? password,
        CancellationToken cancellationToken)
    {
        if (await _users.HasUserWithRoleAsync(RoleNames.Admin, cancellationToken))
            return false;

        if (string.IsNullOrWhiteSpace(email) ||
            !new EmailAddressAttribute().IsValid(email) ||
            string.IsNullOrWhiteSpace(password) ||
            password.Length < 12)
        {
            throw new InvalidOperationException(
                "First-admin bootstrap requires a valid email and a password of at least 12 characters.");
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();
        if (await _users.EmailExistsAsync(normalizedEmail, cancellationToken))
            throw new InvalidOperationException(
                "The configured first-admin email is already assigned to another account.");

        var role = await _users.GetRoleByNameAsync(RoleNames.Admin, cancellationToken)
            ?? throw new InvalidOperationException(
                "Admin role is missing from the database. Apply database migrations first.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = normalizedEmail,
            PasswordHash = _hasher.Hash(password),
            FullName = "Platform Administrator",
            RoleId = role.Id,
            AccountStatus = AccountStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
        await _users.AddAsync(user, cancellationToken);
        return true;
    }

    private static AdminAccountResponse ToResponse(User user) =>
        ToResponse(user, user.Role.Name);

    private static AdminAccountResponse ToResponse(User user, string roleName) =>
        new(
            user.Id,
            user.FullName,
            user.Email,
            roleName,
            user.AccountStatus.ToString(),
            new DateTimeOffset(DateTime.SpecifyKind(user.CreatedAt, DateTimeKind.Utc)));
}
