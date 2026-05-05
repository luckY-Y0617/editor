using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Northstar.Application.Knowledge;
using Northstar.Contracts.Knowledge;
using Northstar.Domain.Knowledge.Comments;
using Northstar.Infrastructure.Persistence;

namespace Northstar.Infrastructure.Knowledge;

public sealed class EfCommentRepository : ICommentRepository
{
    private const string DefaultAnchorStatus = "active";
    private const string UnknownAuthorName = "Unknown";

    private readonly NorthstarDbContext _dbContext;

    public EfCommentRepository(NorthstarDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<CommentThreadDto>> ListThreadDtosAsync(
        Guid documentId,
        CancellationToken cancellationToken = default)
    {
        var threads = await _dbContext.CommentThreads
            .AsNoTracking()
            .Where(thread => thread.DocumentId == documentId)
            .OrderByDescending(thread => thread.CreatedAt)
            .ToListAsync(cancellationToken);

        return await MapThreadDtosAsync(threads, cancellationToken);
    }

    public async Task<CommentThreadDto?> GetThreadDtoAsync(
        Guid documentId,
        Guid threadId,
        CancellationToken cancellationToken = default)
    {
        var thread = await _dbContext.CommentThreads
            .AsNoTracking()
            .Where(thread => thread.DocumentId == documentId && thread.Id == threadId)
            .FirstOrDefaultAsync(cancellationToken);

        if (thread is null)
        {
            return null;
        }

        return (await MapThreadDtosAsync([thread], cancellationToken)).Single();
    }

    public Task<CommentThread?> GetThreadAsync(
        Guid documentId,
        Guid threadId,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.CommentThreads
            .FirstOrDefaultAsync(
                thread => thread.DocumentId == documentId &&
                    thread.Id == threadId,
                cancellationToken);
    }

    public async Task AddThreadAsync(
        CommentThread thread,
        CommentMessage firstMessage,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.CommentThreads.AddAsync(thread, cancellationToken);
        await _dbContext.CommentMessages.AddAsync(firstMessage, cancellationToken);
    }

    public async Task AddMessageAsync(
        CommentMessage message,
        CancellationToken cancellationToken = default)
    {
        await _dbContext.CommentMessages.AddAsync(message, cancellationToken);
    }

    private async Task<IReadOnlyList<CommentThreadDto>> MapThreadDtosAsync(
        IReadOnlyList<CommentThread> threads,
        CancellationToken cancellationToken)
    {
        if (threads.Count == 0)
        {
            return [];
        }

        var threadIds = threads.Select(thread => thread.Id).ToArray();
        var messageRows = await (
            from message in _dbContext.CommentMessages.AsNoTracking()
            join user in _dbContext.Users.AsNoTracking() on message.AuthorUserId equals user.Id into userJoin
            from author in userJoin.DefaultIfEmpty()
            where threadIds.Contains(message.ThreadId)
            orderby message.CreatedAt, message.Id
            select new CommentMessageReadRow(
                message,
                author == null ? UnknownAuthorName : author.DisplayName))
            .ToListAsync(cancellationToken);

        var messagesByThreadId = messageRows
            .GroupBy(row => row.Message.ThreadId)
            .ToDictionary(
                group => group.Key,
                group => (IReadOnlyList<CommentMessageDto>)group.Select(MapMessageDto).ToArray());

        return threads.Select(thread =>
            new CommentThreadDto(
                thread.Id.ToString(),
                thread.DocumentId.ToString(),
                thread.Status,
                DefaultAnchorStatus,
                ParseJson(thread.AnchorJson),
                messagesByThreadId.TryGetValue(thread.Id, out var messages) ? messages : [],
                thread.CreatedAt,
                thread.UpdatedAt,
                thread.ResolvedAt))
            .ToArray();
    }

    private static CommentMessageDto MapMessageDto(CommentMessageReadRow row)
    {
        return new CommentMessageDto(
            row.Message.Id.ToString(),
            row.Message.ThreadId.ToString(),
            row.Message.Body,
            new CommentAuthorDto(row.Message.AuthorUserId.ToString(), row.AuthorDisplayName),
            row.Message.CreatedAt,
            row.Message.UpdatedAt,
            row.Message.DeletedAt);
    }

    private static JsonElement ParseJson(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }

    private sealed record CommentMessageReadRow(CommentMessage Message, string AuthorDisplayName);
}
