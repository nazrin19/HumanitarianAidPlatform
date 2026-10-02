using Microsoft.EntityFrameworkCore;
using HumanitarianAidPlatform.Domain.Entities.Requests;
using HumanitarianAidPlatform.Domain.Entities.Shelters;

namespace HumanitarianAidPlatform.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<AssistanceRequest> AssistanceRequests => Set<AssistanceRequest>();
    public DbSet<Shelter> Shelters => Set<Shelter>();
}
