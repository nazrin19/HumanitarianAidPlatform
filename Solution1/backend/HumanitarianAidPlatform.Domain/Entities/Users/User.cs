using System;
using System.Collections.Generic;
using System.Text;

namespace HumanitarianAidPlatform.Domain.Entities.Users;

public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = default!;
    public string PasswordHash { get; set; } = default!;
    public string FullName { get; set; } = default!;
    public Guid RoleId { get; set; }
    public Role Role { get; set; } = default!;
    public Guid? OrganisationId { get; set; }
    public Organisation? Organisation { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
