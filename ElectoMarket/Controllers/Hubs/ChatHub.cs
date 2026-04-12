using Microsoft.AspNetCore.SignalR;
using System.Threading.Tasks;

namespace ElectoMarket.Hubs
{
  public class ChatHub : Hub
  {
    // Cuando un usuario abre un chat, lo metemos a un "grupo" virtual con el número del Chat
    public async Task UnirseAlChat(string chatId)
    {
      await Groups.AddToGroupAsync(Context.ConnectionId, chatId);
    }
  }
}
