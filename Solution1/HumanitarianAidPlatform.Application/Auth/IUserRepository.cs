using System;
using System.Collections.Generic;
using System.Text;

using HumanitarianAidPlatform.Domain.Entities.Users;

namespace HumanitarianAidPlatform.Application.Auth;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email);
    Task<bool> EmailExistsAsync(string email);
    Task<Role?> GetRoleByNameAsync(string name);
    Task AddAsync(User user);
}
