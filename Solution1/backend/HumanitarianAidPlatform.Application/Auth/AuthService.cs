using System;
using System.Collections.Generic;
using System.Text;

using HumanitarianAidPlatform.Domain.Entities.Donors;
using HumanitarianAidPlatform.Domain.Entities.Users;
using HumanitarianAidPlatform.Domain.Entities.Volunteers;

namespace HumanitarianAidPlatform.Application.Auth;

public class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IPasswordHasher _hasher;
    private readonly IJwtTokenGenerator _jwt;

    public AuthService(IUserRepository users, IPasswordHasher hasher, IJwtTokenGenerator jwt)
    {
        _users = users;
        _hasher = hasher;
        _jwt = jwt;
    }

    public async Task<AuthResult> RegisterAsync(
        RegisterRequestDto dto,
        CancellationToken cancellationToken)
    {
        var email = dto.Email.Trim().ToLowerInvariant();

        if (await _users.EmailExistsAsync(email, cancellationToken))
            return AuthResult.Fail("This email is already registered.");

        var role = await _users.GetRoleByNameAsync(RoleNames.Citizen, cancellationToken);
        if (role is null)
            return AuthResult.Fail("Citizen role is missing from the database.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = _hasher.Hash(dto.Password),
            FullName = dto.FullName.Trim(),
            RoleId = role.Id,
            AccountStatus = AccountStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        await _users.AddAsync(user, cancellationToken);
        return AuthResult.Ok(BuildResponse(user, role.Name));
    }

    public async Task<RegistrationResult> RegisterVolunteerAsync(
        RegisterVolunteerRequest dto,
        CancellationToken cancellationToken)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        if (await _users.EmailExistsAsync(email, cancellationToken))
            return RegistrationResult.Fail("This email is already registered.");

        var role = await _users.GetRoleByNameAsync(RoleNames.Volunteer, cancellationToken);
        if (role is null)
            throw new InvalidOperationException(
                "Volunteer role is missing from the database. Apply database migrations first.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = _hasher.Hash(dto.Password),
            FullName = dto.FullName.Trim(),
            RoleId = role.Id,
            AccountStatus = AccountStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        var volunteer = new Volunteer
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Email = email,
            FullName = user.FullName,
            Phone = dto.Phone?.Trim(),
            Skills = dto.Skills?.Trim(),
            Availability = dto.Availability,
            CreatedAt = user.CreatedAt
        };

        await _users.AddVolunteerApplicantAsync(user, volunteer, cancellationToken);
        return RegistrationResult.Ok(user.Id);
    }

    public async Task<RegistrationResult> RegisterDonorAsync(
        RegisterDonorRequest dto,
        CancellationToken cancellationToken)
    {
        var email = dto.Email.Trim().ToLowerInvariant();
        if (await _users.EmailExistsAsync(email, cancellationToken))
            return RegistrationResult.Fail("This email is already registered.");

        var role = await _users.GetRoleByNameAsync(RoleNames.Donor, cancellationToken);
        if (role is null)
            throw new InvalidOperationException(
                "Donor role is missing from the database. Apply database migrations first.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = _hasher.Hash(dto.Password),
            FullName = dto.FullName.Trim(),
            RoleId = role.Id,
            AccountStatus = AccountStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };
        var donor = new Donor
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Email = email,
            FullName = user.FullName,
            Phone = dto.Phone?.Trim(),
            Type = dto.Type,
            IsActive = false,
            CreatedAt = user.CreatedAt
        };

        await _users.AddDonorApplicantAsync(user, donor, cancellationToken);
        return RegistrationResult.Ok(user.Id);
    }

    public async Task<AuthResult> LoginAsync(
        LoginRequestDto dto,
        CancellationToken cancellationToken)
    {
        var user = await _users.GetByEmailAsync(
            dto.Email.Trim().ToLowerInvariant(),
            cancellationToken);

        // Same message for "no such user" and "wrong password" so attackers can't tell which emails exist.
        if (user is null || !_hasher.Verify(dto.Password, user.PasswordHash))
            return AuthResult.Fail("Invalid email or password.");

        if (user.AccountStatus != AccountStatus.Active)
            return AuthResult.Fail(
                user.AccountStatus == AccountStatus.Pending
                    ? "Your application is awaiting administrator approval."
                    : "This account is not active.");

        return AuthResult.Ok(BuildResponse(user, user.Role.Name));
    }

    private AuthResponseDto BuildResponse(User user, string roleName)
    {
        var (token, expiresAt) = _jwt.Generate(user, roleName);
        return new AuthResponseDto(token, expiresAt, user.Email, user.FullName, roleName);
    }
}
