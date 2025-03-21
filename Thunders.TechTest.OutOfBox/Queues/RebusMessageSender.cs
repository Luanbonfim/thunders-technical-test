using Rebus.Bus;

namespace Thunders.TechTest.OutOfBox.Queues
{
    public class RebusMessageSender(IBus bus) : IMessageSender
    {
        public virtual async Task SendLocal(object message)
        {
            await bus.SendLocal(message);
        }
        
        public virtual async Task Publish(object message)
        {
            await bus.Publish(message);
        }
    }
}
