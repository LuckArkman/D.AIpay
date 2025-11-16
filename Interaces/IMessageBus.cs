namespace Interaces;

public interface IMessageBus
{
    void Publish(object data, string routingKey);
}