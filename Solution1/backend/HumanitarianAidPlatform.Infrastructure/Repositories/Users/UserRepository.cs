using System;
using System.Collections.Generic;
using System.Text;

using HumanitarianAidPlatform.Application.Auth;
using HumanitarianAidPlatform.Domain.Entities.Donors;
using HumanitarianAidPlatform.Domain.Entities.Users;
using HumanitarianAidPlatform.Domain.Entities.Volunteers;
using HumanitarianAidPlatform.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HumanitarianAidPlatform.Infrastructure.Repositories.Users;

public class UserRepository : IUserRepository
{
    private readonly AppDbContext _db;

    public UserRepository(AppDbContext db)
    {
        _db = db;
    }

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken) =>
        _db.Users.Include(u => u.Role).FirstOrDefaultAsync(
            u => u.Email == email,
            cancellationToken);

    public async Task<bool> EmailExistsAsync(
        string email,
        CancellationToken cancellationToken) =>
        await _db.Users.AnyAsync(u => u.Email == email, cancellationToken) ||
        await _db.Volunteers.AnyAsync(v => v.Email == email, cancellationToken) ||
        await _db.Donors.AnyAsync(d => d.Email == email, cancellationToken);

    public Task<Role?> GetRoleByNameAsync(string name, CancellationToken cancellationToken) =>
        _db.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Name == name, cancellationToken);

    public async Task AddAsync(User user, CancellationToken cancellationToken)
    {
        _db.Users.Add(user);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task AddVolunteerApplicantAsync(
        User user,
        Volunteer volunteer,
        CancellationToken cancellationToken)
    {
        _db.Users.Add(user);
        _db.Volunteers.Add(volunteer);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task AddDonorApplicantAsync(
        User user,
        Donor donor,
        CancellationToken cancellationToken)
    {
        _db.Users.Add(user);
        _db.Donors.Add(donor);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task<bool> HasUserWithRoleAsync(
        string roleName,
        CancellationToken cancellationToken) =>
        _db.Users.AnyAsync(u => u.Role.Name == roleName, cancellationToken);

    public async Task<IReadOnlyList<User>> GetPendingApplicantsAsync(
        CancellationToken cancellationToken) =>
        await _db.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .Include(u => u.VolunteerProfile)
            .Include(u => u.DonorProfile)
            .Where(u => u.AccountStatus == AccountStatus.Pending &&
                        (u.Role.Name == RoleNames.Volunteer ||
                         u.Role.Name == RoleNames.Donor))
            .OrderBy(u => u.CreatedAt)
            .ToListAsync(cancellationToken);

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken) =>
        _db.Users
            .Include(u => u.Role)
            .Include(u => u.VolunteerProfile)
            .Include(u => u.DonorProfile)
            .FirstOrDefaultAsync(u => u.Id == id, cancellationToken);

    public async Task UpdateAsync(User user, CancellationToken cancellationToken)
    {
        _db.Users.Update(user);
        await _db.SaveChangesAsync(cancellationToken);
    }
}
