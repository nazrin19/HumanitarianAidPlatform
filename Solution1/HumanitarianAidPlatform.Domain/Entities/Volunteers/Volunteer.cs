using System;
using System.Collections.Generic;
using System.Text;

namespace HumanitarianAidPlatform.Domain.Entities.Volunteers
{
    internal class Volunteer
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string? Phone { get; set; }
        public string? Skills { get; set; }        // e.g. "First aid, Driving"
        public string Availability { get; set; } = "Available"; // Available, Busy, Inactive
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<VolunteerTask> Tasks { get; set; } = new();
    }
}
