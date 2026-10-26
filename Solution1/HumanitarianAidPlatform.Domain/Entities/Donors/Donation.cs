using System;
using System.Collections.Generic;
using System.Text;

namespace HumanitarianAidPlatform.Domain.Entities.Donors
{
    internal class Donation
    {
        public Guid Id { get; set; }
        public Guid DonorId { get; set; }
        public Donor Donor { get; set; } = default!;
        public Guid? CampaignId { get; set; }
        public Campaign? Campaign { get; set; }
        public string Type { get; set; } = "Monetary"; // Monetary, InKind
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "LKR";
        public string? Description { get; set; }
        public DateTime DonatedAt { get; set; } = DateTime.UtcNow;
    }
}
