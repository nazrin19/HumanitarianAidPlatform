using System;
using HumanitarianAidPlatform.Domain.Entities.Requests;
using HumanitarianAidPlatform.Domain.Entities.Shelters;
using HumanitarianAidPlatform.Domain.Entities.Notifications;
using HumanitarianAidPlatform.Domain.Entities.Resources;
using HumanitarianAidPlatform.Domain.Entities.Users;
using HumanitarianAidPlatform.Domain.Entities.Donors;
using HumanitarianAidPlatform.Domain.Entities.Volunteers;
using Microsoft.EntityFrameworkCore;

namespace HumanitarianAidPlatform.Infrastructure.Persistence;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<AssistanceRequest> AssistanceRequests =>
        Set<AssistanceRequest>();

    public DbSet<RequestStatusHistory> RequestStatusHistories =>
        Set<RequestStatusHistory>();

    public DbSet<AiRecommendation> AiRecommendations =>
        Set<AiRecommendation>();

    public DbSet<AuditLog> AuditLogs =>
        Set<AuditLog>();

    public DbSet<Shelter> Shelters =>
        Set<Shelter>();

    public DbSet<Resource> Resources =>
        Set<Resource>();

    public DbSet<ResourceAllocation> ResourceAllocations =>
        Set<ResourceAllocation>();

    public DbSet<Notification> Notifications =>
        Set<Notification>();

    public DbSet<User> Users =>
        Set<User>();

    public DbSet<Role> Roles =>
        Set<Role>();

    public DbSet<Organisation> Organisations =>
        Set<Organisation>();

    // --- Donor & Volunteer modules ---
    public DbSet<Donor> Donors => Set<Donor>();

    public DbSet<Donation> Donations => Set<Donation>();

    public DbSet<Campaign> Campaigns => Set<Campaign>();

    public DbSet<Volunteer> Volunteers => Set<Volunteer>();

    public DbSet<VolunteerTask> VolunteerTasks => Set<VolunteerTask>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Assistance request configuration
        modelBuilder.Entity<AssistanceRequest>(r =>
        {
            r.Property(x => x.Category)
                .HasConversion<string>()
                .HasMaxLength(30);

            r.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(30);

            r.Property(x => x.Description)
                .HasMaxLength(2000);

            r.Property(x => x.Address)
                .HasMaxLength(300);

            r.HasIndex(x => x.CitizenId);
            r.HasIndex(x => x.Status);

            r.HasMany(x => x.StatusHistory)
                .WithOne()
                .HasForeignKey(h => h.AssistanceRequestId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AuditLog>(a =>
        {
            a.Property(x => x.Action)
                .HasConversion<string>()
                .HasMaxLength(30);

            a.Property(x => x.AiSnapshotJson)
                .HasColumnType("nvarchar(max)")
                .IsRequired();

            a.Property(x => x.ChangedValuesJson)
                .HasColumnType("nvarchar(max)");

            a.Property(x => x.Reason)
                .HasMaxLength(1000);

            a.HasIndex(x => x.AssistanceRequestId);
            a.HasIndex(x => x.DecidedByUserId);
            a.HasIndex(x => x.DecidedAt);

            a.HasOne<AssistanceRequest>()
                .WithMany()
                .HasForeignKey(x => x.AssistanceRequestId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Request status history configuration
        modelBuilder.Entity<RequestStatusHistory>(h =>
        {
            h.Property(x => x.FromStatus)
                .HasConversion<string>()
                .HasMaxLength(30);

            h.Property(x => x.ToStatus)
                .HasConversion<string>()
                .HasMaxLength(30);

            h.Property(x => x.Note)
                .HasMaxLength(500);
        });

        // AI recommendation configuration
        modelBuilder.Entity<AiRecommendation>(a =>
        {
            a.Property(x => x.SuggestedCategory).HasConversion<string>().HasMaxLength(30);
            a.Property(x => x.UrgencyLevel).HasConversion<string>().HasMaxLength(30);
            a.Property(x => x.Rationale).HasMaxLength(2000);
            a.Property(x => x.ModelName).HasMaxLength(50);
            a.HasIndex(x => x.AssistanceRequestId).IsUnique(); // one recommendation per request for now
            a.HasOne<AssistanceRequest>()
                .WithMany()
                .HasForeignKey(x => x.AssistanceRequestId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Resource allocation configuration
        modelBuilder.Entity<ResourceAllocation>(a =>
        {
            a.HasKey(x => x.Id);

            a.Property(x => x.QuantityAllocated)
                .IsRequired();

            // Each allocation belongs to one resource
            a.HasOne<Resource>()
                .WithMany()
                .HasForeignKey(x => x.ResourceId)
                .OnDelete(DeleteBehavior.Restrict);

            // Each allocation belongs to one assistance request
            a.HasOne<AssistanceRequest>()
                .WithMany()
                .HasForeignKey(x => x.AssistanceRequestId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<Notification>(n =>
        {
            n.Property(x => x.Channel)
                .HasConversion(
                    channel => ConvertNotificationChannelToDatabase(channel),
                    value => ConvertNotificationChannelFromDatabase(value))
                .HasColumnName("Type")
                .HasMaxLength(30)
                .IsRequired();
        });

        // User configuration
        modelBuilder.Entity<User>(u =>
        {
            u.Property(x => x.Email)
                .HasMaxLength(256)
                .IsRequired();

            u.Property(x => x.AccountStatus)
                .HasConversion<string>()
                .HasMaxLength(20)
                .HasDefaultValue(AccountStatus.Active);

            u.HasIndex(x => x.Email)
                .IsUnique();
        });

        // Role configuration and seed data
        modelBuilder.Entity<Role>(r =>
        {
            r.Property(x => x.Name)
                .HasMaxLength(50)
                .IsRequired();

            r.HasIndex(x => x.Name)
                .IsUnique();

            r.HasData(
                new Role
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                    Name = RoleNames.Citizen
                },
                new Role
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000010"),
                    Name = RoleNames.Volunteer
                },
                new Role
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000011"),
                    Name = RoleNames.Donor
                },
                new Role
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                    Name = RoleNames.Coordinator
                },
                new Role
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000003"),
                    Name = RoleNames.CaseManager
                },
                new Role
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000004"),
                    Name = RoleNames.ResourceManager
                },
                new Role
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000005"),
                    Name = RoleNames.ShelterAdmin
                },
                new Role
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000006"),
                    Name = RoleNames.VolunteerCoordinator
                },
                new Role
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000007"),
                    Name = RoleNames.DonorOfficer
                },
                new Role
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000008"),
                    Name = RoleNames.PartnerOfficer
                },
                new Role
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000009"),
                    Name = RoleNames.Admin
                });
        });

        // Donor & Volunteer configuration
        modelBuilder.Entity<Donation>(d =>
        {
            d.Property(x => x.Amount).HasPrecision(18, 2);
            d.Property(x => x.Currency).HasMaxLength(3);
        });

        modelBuilder.Entity<Donor>(d =>
        {
            d.Property(x => x.FullName).HasMaxLength(150).IsRequired();
            d.Property(x => x.Email).HasMaxLength(256).IsRequired();
            d.HasIndex(x => x.Email).IsUnique();
        });

        modelBuilder.Entity<Volunteer>(v =>
        {
            v.Property(x => x.FullName).HasMaxLength(150).IsRequired();
            v.Property(x => x.Email).HasMaxLength(256).IsRequired();
            v.HasIndex(x => x.Email).IsUnique();
            v.HasIndex(x => x.UserId).IsUnique().HasFilter("[UserId] IS NOT NULL");
            v.HasOne(x => x.User)
                .WithOne(x => x.VolunteerProfile)
                .HasForeignKey<Volunteer>(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Donor>(d =>
        {
            d.HasIndex(x => x.UserId).IsUnique().HasFilter("[UserId] IS NOT NULL");
            d.HasOne(x => x.User)
                .WithOne(x => x.DonorProfile)
                .HasForeignKey<Donor>(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Donor & Volunteer relationships
        modelBuilder.Entity<Donation>(d =>
        {
            d.HasOne(x => x.Donor).WithMany(x => x.Donations)
                .HasForeignKey(x => x.DonorId).OnDelete(DeleteBehavior.Restrict);
            d.HasOne(x => x.Campaign).WithMany(x => x.Donations)
                .HasForeignKey(x => x.CampaignId).OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<VolunteerTask>(t =>
        {
            t.Property(x => x.Title).HasMaxLength(200).IsRequired();
            t.Property(x => x.Status).HasMaxLength(20);
            t.HasOne(x => x.Volunteer).WithMany(x => x.Tasks)
                .HasForeignKey(x => x.VolunteerId).OnDelete(DeleteBehavior.SetNull);
        });
    }

    private static string ConvertNotificationChannelToDatabase(NotificationChannel channel)
    {
        return channel switch
        {
            NotificationChannel.Email => "Email",
            NotificationChannel.Sms => "SMS",
            NotificationChannel.InApp => "InApp",
            _ => "Unknown"
        };
    }

    private static NotificationChannel ConvertNotificationChannelFromDatabase(string value)
    {
        if (value.Equals("SMS", StringComparison.OrdinalIgnoreCase))
        {
            return NotificationChannel.Sms;
        }

        return Enum.TryParse<NotificationChannel>(value, true, out var channel)
            ? channel
            : NotificationChannel.Unknown;
    }
}