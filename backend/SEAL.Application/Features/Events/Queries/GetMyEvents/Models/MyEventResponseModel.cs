using SEAL_Application.Features.Events.Models;

namespace SEAL_Application.Features.Events.Queries.GetMyEvents.Models
{
    public class MyEventResponseModel : EventModel
    {
        public string Role { get; set; } = string.Empty;
        public string? TeamId { get; set; }
    }
}
