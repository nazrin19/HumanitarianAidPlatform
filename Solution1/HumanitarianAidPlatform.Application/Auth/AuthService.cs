using System;
using System.Collections.Generic;
using System.Text;

using HumanitarianAidPlatform.Domain.Entities.Users;

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

    public async Task<AuthResult> RegisterAsync(RegisterRequestDto dto)
    {
        var email = dto.Email.Trim().ToLowerInvariant();

        if (await _users.EmailExistsAsync(email))
            return AuthResult.Fail("This email is already registered.");

        // Public sign-up always creates a Citizen. Staff accounts are created by an Admin later.
        var role = await _users.GetRoleByNameAsync(RoleNames.Citizen);
        if (role is null)
            return AuthResult.Fail("Citizen role is missing from the database.");

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            PasswordHash = _hasher.Hash(dto.Password),
            FullName = dto.FullName.Trim(),
            RoleId = role.Id,
            CreatedAt = DateTime.UtcNow
        };

        await _users.AddAsync(user);
        return AuthResult.Ok(BuildResponse(user, role.Name));
    }

    public async Task<AuthResult> LoginAsync(LoginRequestDto dto)
    {
        var user = await _users.GetByEmailAsync(dto.Email.Trim().ToLowerInvariant());

        // Same message for "no such user" and "wrong password" so attackers can't tell which emails exist.
        if (user is null || !_hasher.Verify(dto.Password, user.PasswordHash))
            return AuthResult.Fail("Invalid email or password.");

        return AuthResult.Ok(BuildResponse(user, user.Role.Name));
    }

    private AuthResponseDto BuildResponse(User user, string roleName)
    {
        var (token, expiresAt) = _jwt.Generate(user, roleName);
        return new AuthResponseDto(token, expiresAt, user.Email, user.FullName, roleName);
    }
}
