using System;

namespace SEAL_Application.Features.Events.Commands.UpdateEvent.Models
{
    public class UpdateEventResponseModel
    {
        public string Id { get; set; } = string.Empty;
        public string EventName { get; set; } = string.Empty;
        public string? Season { get; set; }
        public int Year { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public DateTime? RegistrationStartDate { get; set; }
        public DateTime? RegistrationEndDate { get; set; }
        public string? Description { get; set; }
        public bool Status { get; set; }
        public string? PhotoEventUrl { get; set; }
        public DateTimeOffset LastUpdatedTime { get; set; }
    }
}
