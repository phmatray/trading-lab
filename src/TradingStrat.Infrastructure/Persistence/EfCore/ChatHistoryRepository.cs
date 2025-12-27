using Microsoft.EntityFrameworkCore;
using TradingStrat.Application.Ports.Outbound;
using TradingStrat.Domain.Entities;

namespace TradingStrat.Infrastructure.Persistence.EfCore;

/// <summary>
/// EF Core repository for chat message persistence.
/// Implements IChatHistoryPort to store and retrieve conversation history.
/// </summary>
public class ChatHistoryRepository : IChatHistoryPort
{
    private readonly TradingContext _context;

    public ChatHistoryRepository(TradingContext context)
    {
        _context = context;
    }

    public async Task SaveMessageAsync(ChatMessage message)
    {
        _context.ChatMessages.Add(message);
        await _context.SaveChangesAsync();
    }

    public async Task<List<ChatMessage>> GetConversationHistoryAsync(string sessionId, int limit = 20)
    {
        return await _context.ChatMessages
            .Where(m => m.SessionId == sessionId)
            .OrderByDescending(m => m.Timestamp)
            .Take(limit)
            .OrderBy(m => m.Timestamp) // Re-order chronologically for conversation context
            .ToListAsync();
    }

    public async Task ClearHistoryAsync(string sessionId)
    {
        List<ChatMessage> messages = await _context.ChatMessages
            .Where(m => m.SessionId == sessionId)
            .ToListAsync();

        _context.ChatMessages.RemoveRange(messages);
        await _context.SaveChangesAsync();
    }
}
