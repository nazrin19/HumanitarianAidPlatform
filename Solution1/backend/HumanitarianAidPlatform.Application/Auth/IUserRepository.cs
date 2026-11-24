using System;
using System.Collections.Generic;
using System.Text;

using HumanitarianAidPlatform.Domain.Entities.Users;
using HumanitarianAidPlatform.Domain.Entities.Donors;
using HumanitarianAidPlatform.Domain.Entities.Volunteers;
using HumanitarianAidPlatform.Application.Admin;

namespace HumanitarianAidPlatform.Application.Auth;

public interface IUserRepository
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken);
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken);
    Task<Role?> GetRoleByNameAsync(string name, CancellationToken cancellationToken);
    Task AddAsync(User user, CancellationToken cancellationToken);
    Task AddVolunteerApplicantAsync(User user, Volunteer volunteer, CancellationToken cancellationToken);
    Task AddDonorApplicantAsync(User user, Donor donor, CancellationToken cancellationToken);
    Task<bool> HasUserWithRoleAsync(string roleName, CancellationToken cancellationToken);
    Task<IReadOnlyList<User>> GetPendingApplicantsAsync(CancellationToken cancellationToken);
    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken);
    Task UpdateAsync(User user, CancellationToken cancellationToken);
}
