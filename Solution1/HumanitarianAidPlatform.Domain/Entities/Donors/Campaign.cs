using System;
using System.Collections.Generic;
using System.Text;

namespace HumanitarianAidPlatform.Domain.Entities.Donors
{
    internal class Campaign
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = default!;
        public string? Description { get; set; }
        public decimal TargetAmount { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public bool IsActive { get; set; } = true;

        public List<Donation> Donations { get; set; } = new();
    }
}
