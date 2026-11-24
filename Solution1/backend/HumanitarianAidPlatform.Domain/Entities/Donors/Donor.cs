using System;
using System.Collections.Generic;
using System.Text;
using HumanitarianAidPlatform.Domain.Entities.Users;

namespace HumanitarianAidPlatform.Domain.Entities.Donors
{
    public class Donor
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string? Phone { get; set; }
        public string Type { get; set; } = "Individual"; // Individual, Corporate
        public bool IsActive { get; set; } = true;
        public Guid? UserId { get; set; }
        public User? User { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<Donation> Donations { get; set; } = new();

    }
}

