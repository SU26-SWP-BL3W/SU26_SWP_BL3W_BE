using SEAL_Domain.EventBus;

namespace SEAL_Domain.EventBus
{
    public interface IEventBus
    {
        event EventHandler<Event>? EventPublished;
        void Publish(Event @event);
    }
}
