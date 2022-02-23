using System.Threading.Tasks;
using ConversationalAI.Infrastructure.Interfaces.Repositories;
using ConversationalAI.Mediator.Hubs;
using ConversationalAI.Mediator.Infrastructure.Services.Infrastructure;
using Microsoft.AspNetCore.SignalR;

namespace ConversationalAI.Mediator.Infrastructure.Services
{
    public class HubContextService : IHubContextService
    {
        private readonly IHubContext<ConversationalHub> _hubContext;
        private readonly IHubRepository _hubRepository;

        public HubContextService(IHubContext<ConversationalHub> hubContext,
            IHubRepository hubRepository)
        {
            _hubContext = hubContext;
            _hubRepository = hubRepository;
        }

        public async Task Broadcast(string connectionId, string user, string message, string originalMessage)
        {
            await _hubRepository.UpdateConnection(connectionId, user);
            
            var connection = await _hubRepository.GetConnectionId(user);
            
            if (connectionId != null && connection == connectionId) 
                await _hubContext.Clients.Client(connection).SendAsync("ReceiveBroadcast", user, message, originalMessage);
        }
        
        public async Task Broadcast(string user, string message, string originalMessage)
        {
            var connection = await _hubRepository.GetConnectionId(user);
            if (connection is { Length: > 0 })
                await _hubRepository.UpdateConnection(connection, user);
            
            await _hubContext.Clients.All.SendAsync("ReceiveBroadcast", user, message, originalMessage);
        }
    }
}