namespace SEAL_Application.Features.EventCoordinators.Commands.InviteEventCoordinator.Models
{
    public class InviteEventCoordinatorRequestModel
    {
        public string EventId { get; set; } = string.Empty;
        public string CoordinatorEmail { get; set; } = string.Empty;
        public string CoordinatorFullName { get; set; } = string.Empty;
        public string? Notes { get; set; }
    }
}
