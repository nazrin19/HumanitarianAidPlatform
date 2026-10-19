using Microsoft.EntityFrameworkCore;
using HumanitarianAidPlatform.Domain.Entities.Requests;
using HumanitarianAidPlatform.Domain.Entities.Shelters;
using HumanitarianAidPlatform.Domain.Entities.Notifications; // 1. Add this namespace reference

namespace HumanitarianAidPlatform.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<AssistanceRequest> AssistanceRequests => Set<AssistanceRequest>();
    public DbSet<Shelter> Shelters => Set<Shelter>();
    public DbSet<Notification> Notifications => Set<Notification>(); // 2. Add this DbSet line
}