using System;
using System.Collections.Generic;
using System.Text;

namespace HumanitarianAidPlatform.Domain.Entities.Volunteers
{
    internal class VolunteerTask
    {
        public Guid Id { get; set; }
        public Guid? VolunteerId { get; set; }
        public Volunteer? Volunteer { get; set; }
        public string Title { get; set; } = default!;
        public string? Description { get; set; }
        public string? Location { get; set; }
        public string Status { get; set; } = "Open"; // Open, Assigned, Completed
        public DateTime? DueDate { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
