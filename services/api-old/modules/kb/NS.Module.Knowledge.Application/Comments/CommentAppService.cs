using System.Text.Json;
using NS.Framework.Authorization.AspNetCore;
using NS.Module.Identity.Application.Contracts.Users;
using NS.Module.Identity.Application.Contracts.Users.Dtos;
using NS.Module.Knowledge.Application.Contracts.Comments;
using NS.Module.Knowledge.Application.Contracts.Comments.Dtos;
using NS.Module.Knowledge.Domain.Comments;
using NS.Module.Knowledge.Domain.Documents;
using NS.Module.Knowledge.Domain.KnowledgeBases;
using NS.Module.Knowledge.Domain.Shared.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Authorization;
using Volo.Abp.EventBus.Local;
using Volo.Abp.Users;

namespace NS.Module.Knowledge.Application.Comments;

[Authorize]
[ApiController]
[Route("/api/app/kbs/{baseId:guid}/documents/{documentId:guid}/comments")]
public class CommentAppService : ApplicationService, ICommentAppService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    private readonly ICommentRepository _commentRepository;
    private readonly CommentManager _commentManager;
    private readonly IAuthUserProfileProvider _userProfileProvider;
    private readonly IDocumentRepository _documentRepository;
    private readonly IKnowledgeBaseRepository _kbRepository;
    private readonly ILocalEventBus _localEventBus;

    public CommentAppService(
        ICommentRepository commentRepository,
        CommentManager commentManager,
        IAuthUserProfileProvider userProfileProvider,
        IDocumentRepository documentRepository,
        IKnowledgeBaseRepository kbRepository,
        ILocalEventBus localEventBus)
    {
        _commentRepository = commentRepository;
        _commentManager = commentManager;
        _userProfileProvider = userProfileProvider;
        _documentRepository = documentRepository;
        _kbRepository = kbRepository;
        _localEventBus = localEventBus;
    }


    [HttpGet]
    [RequirePermission(KnowledgePermissions.Comment.View)]
    public async Task<List<CommentDto>> GetListAsync(Guid baseId, Guid documentId)
    {
        var comments = await _commentRepository.GetByDocumentIdAsync(documentId);

        var creatorIds = comments
            .Select(c => c.CreatorId)
            .Where(id => id.HasValue)
            .Select(id => id!.Value)
            .Distinct()
            .ToList();

        Dictionary<Guid, UserLookupDto> userMap = new();
        if (creatorIds.Count > 0)
        {
            var users = await _userProfileProvider.FindByIdsAsync(creatorIds);
            userMap = users.ToDictionary(u => u.Id, u => u);
        }

        // 先映射为 dto（平铺）
        var dtoDict = comments.ToDictionary(
            c => c.Id,
            c =>
            {
                var dto = ObjectMapper.Map<Comment, CommentDto>(c);

                // positionJson -> JsonElement?（返回给前端）
                dto.Position = TryParseJsonElement(c.PositionJson);

                if (c.CreatorId.HasValue &&
                    userMap.TryGetValue(c.CreatorId.Value, out var creator))
                {
                    dto.CreatorName = creator.UserName;
                    dto.AvatarUrl = creator.AvatarUrl;
                }

                dto.Replies = new List<CommentDto>();
                return dto;
            });

        // 组装树
        foreach (var c in comments)
        {
            if (c.ParentId.HasValue &&
                dtoDict.TryGetValue(c.ParentId.Value, out var parent))
            {
                parent.Replies.Add(dtoDict[c.Id]);
            }
        }

        // 根节点排序（你也可以按 CreationTime 正序）
        var roots = dtoDict.Values
            .Where(d => d.ParentId == null)
            .OrderByDescending(d => d.CreationTime)
            .ToList();

        return roots;
    }

    // -----------------------------
    // 创建评论
    // -----------------------------
    [HttpPost]
    [RequirePermission(KnowledgePermissions.Comment.Create)]
    public async Task<CommentDto> CreateAsync(Guid baseId, Guid documentId, CreateCommentRequestDto input)
    {
        // 统一：position 直接存 JSON（后端不解析、不做锚点有效性判断）
        var positionJson = NormalizePositionJson(input.Position);

        var comment = _commentManager.Create(
            documentId: documentId,
            content: input.Content,
            positionJson: positionJson,
            parentId: input.ParentId);

        await _commentRepository.InsertAsync(comment, autoSave: true);

        var dto = ObjectMapper.Map<Comment, CommentDto>(comment);
        dto.Position = TryParseJsonElement(comment.PositionJson);
        dto.Replies = new List<CommentDto>();

        if (dto.CreatorId.HasValue)
        {
            var users = await _userProfileProvider.FindByIdsAsync(new[] { dto.CreatorId.Value });
            var creator = users.FirstOrDefault();
            if (creator != null)
            {
                dto.CreatorName = creator.UserName;
                dto.AvatarUrl = creator.AvatarUrl;
            }
        }

        // 发布评论创建活动事件
        var doc = await _documentRepository.FindAsync(documentId, includeDetails: false);
        var kb = doc != null ? await _kbRepository.FindAsync(doc.KnowledgeBaseId, false) : null;

        return dto;
    }

    // -----------------------------
    // 删除评论（仅作者可删；是否允许删带回复的评论由你决定）
    // -----------------------------
    [HttpDelete("{id:guid}")]
    [RequirePermission(KnowledgePermissions.Comment.Delete)]
    public async Task DeleteAsync(Guid baseId, Guid documentId, Guid id)
    {
        var comment = await _commentRepository.GetAsync(id, includeDetails: false);

        if (comment.DocumentId != documentId)
            throw new BusinessException("Comment:DocumentMismatch");

        if (comment.CreatorId != CurrentUser.GetId())
            throw new AbpAuthorizationException("Not allowed to delete this comment.");

        await _commentRepository.DeleteAsync(comment, autoSave: true);
    }

    // -----------------------------
    // Helpers
    // -----------------------------

    private static JsonElement? TryParseJsonElement(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            using var doc = JsonDocument.Parse(json);
            // Clone：避免 JsonDocument dispose 后 element 失效
            return doc.RootElement.Clone();
        }
        catch
        {
            // 数据脏了就当无锚点（前端就不会高亮）
            return null;
        }
    }

    private static string? NormalizePositionJson(JsonElement? position)
    {
        if (position is null)
            return null;

        // 允许 position = null / undefined
        var element = position.Value;
        if (element.ValueKind is JsonValueKind.Undefined or JsonValueKind.Null)
            return null;

        // 直接序列化回 string：保证存储的是一个完整 JSON 对象
        //（你前端现在就是 {schema,type,blockId,quote,occurrence,hint}）
        try
        {
            return JsonSerializer.Serialize(element, JsonOptions);
        }
        catch
        {
            // 不让异常把接口打爆：当作无 position
            return null;
        }
    }
}
