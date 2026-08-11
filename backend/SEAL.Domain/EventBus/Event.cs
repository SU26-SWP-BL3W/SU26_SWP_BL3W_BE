namespace SEAL_Domain.EventBus
{
    public class Event
    {
        public EventType EventType { get; set; }
    }
    public enum EventType
    {
        UserRoleChanged,
        PermissionChanged
    }
}
