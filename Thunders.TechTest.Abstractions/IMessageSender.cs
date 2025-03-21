namespace Thunders.TechTest.Abstractions
{
    public interface IMessageSender
    {
        Task Send(object message);
    }
}
