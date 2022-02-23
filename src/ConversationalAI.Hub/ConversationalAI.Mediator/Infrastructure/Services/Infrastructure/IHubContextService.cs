using System.Threading.Tasks;

namespace ConversationalAI.Mediator.Infrastructure.Services.Infrastructure
{
    public interface IHubContextService
    {
        Task Broadcast(string connectionId, string user, string message, string originalMessage);
        Task Broadcast(string user, string message, string originalMessage);
    }
}