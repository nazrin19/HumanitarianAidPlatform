using System;
using System.Collections.Generic;
using System.Text;

using HumanitarianAidPlatform.Application.Auth;
using HumanitarianAidPlatform.Domain.Entities.Users;
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

    public Task<User?> GetByEmailAsync(string email) =>
        _db.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.Email == email);

    public Task<bool> EmailExistsAsync(string email) =>
        _db.Users.AnyAsync(u => u.Email == email);

    public Task<Role?> GetRoleByNameAsync(string name) =>
        _db.Roles.AsNoTracking().FirstOrDefaultAsync(r => r.Name == name);

    public async Task AddAsync(User user)
    {
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
    }
}
