
using HumanitarianAidPlatform.Domain.Entities.Requests;
using HumanitarianAidPlatform.Domain.Entities.Shelters;
using HumanitarianAidPlatform.Domain.Entities.Resources;
using HumanitarianAidPlatform.Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;

namespace HumanitarianAidPlatform.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<AssistanceRequest> AssistanceRequests => Set<AssistanceRequest>();
    public DbSet<Shelter> Shelters => Set<Shelter>();
    public DbSet<Resource> Resources => Set<Resource>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Organisation> Organisations => Set<Organisation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(u =>
        {
            u.Property(x => x.Email).HasMaxLength(256).IsRequired();
            u.HasIndex(x => x.Email).IsUnique();
        });

        modelBuilder.Entity<Role>(r =>
        {
            r.Property(x => x.Name).HasMaxLength(50).IsRequired();
            r.HasIndex(x => x.Name).IsUnique();
            r.HasData(
                new Role { Id = Guid.Parse("00000000-0000-0000-0000-000000000001"), Name = RoleNames.Citizen },
                new Role { Id = Guid.Parse("00000000-0000-0000-0000-000000000002"), Name = RoleNames.Coordinator },
                new Role { Id = Guid.Parse("00000000-0000-0000-0000-000000000003"), Name = RoleNames.CaseManager },
                new Role { Id = Guid.Parse("00000000-0000-0000-0000-000000000004"), Name = RoleNames.ResourceManager },
                new Role { Id = Guid.Parse("00000000-0000-0000-0000-000000000005"), Name = RoleNames.ShelterAdmin },
                new Role { Id = Guid.Parse("00000000-0000-0000-0000-000000000006"), Name = RoleNames.VolunteerCoordinator },
                new Role { Id = Guid.Parse("00000000-0000-0000-0000-000000000007"), Name = RoleNames.DonorOfficer },
                new Role { Id = Guid.Parse("00000000-0000-0000-0000-000000000008"), Name = RoleNames.PartnerOfficer },
                new Role { Id = Guid.Parse("00000000-0000-0000-0000-000000000009"), Name = RoleNames.Admin });
        });
    }
}
