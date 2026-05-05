using NS.Module.Knowledge.Domain.Shared.Enums;
using NS.Framework.Core.Utilities.Slug;
using Volo.Abp;
using Volo.Abp.Domain.Services;

namespace NS.Module.Knowledge.Domain.KnowledgeBases;

public class KnowledgeBaseManager : DomainService
{
    private readonly IKnowledgeBaseRepository _kbRepository;

    public KnowledgeBaseManager(IKnowledgeBaseRepository kbRepository)
    {
        _kbRepository = kbRepository;
    }

    public virtual async Task<KnowledgeBase> CreateAsync(
        string name,
        Guid ownerId,
        Guid? teamId = null,
        string? description = null,
        string? icon = null,
        bool allowMembersCreateDoc = false,
        KnowledgeBaseVisibility visibility = KnowledgeBaseVisibility.Private,
        CancellationToken cancellationToken = default)
    {
        Check.NotNullOrWhiteSpace(name, nameof(name));
        Check.NotNull(ownerId, nameof(ownerId));

        var slug = await MakeUniqueSlugAsync(SlugHelper.FromName(name), cancellationToken);

        var kb = new KnowledgeBase(name, ownerId);

        kb.SetDescription(description);
        kb.SetVisibility(visibility);
        kb.SetSortOrder(0);
        kb.SetCode(slug);
        kb.SetIcon(icon);
        kb.SetAllowMembersCreateDoc(allowMembersCreateDoc);
        kb.SetTeamId(teamId);
        kb.SetOwnerId(ownerId);

        return await _kbRepository.InsertAsync(kb, autoSave: true, cancellationToken);
    }

    public virtual async Task UpdateAsync(
        KnowledgeBase kb,
        string? name,
        string? description,
        KnowledgeBaseVisibility? visibility,
        CancellationToken cancellationToken = default)
    {
        if (!name.IsNullOrWhiteSpace())
        {
            kb.SetName(name);
        }

        kb.SetDescription(description);

        if (visibility.HasValue)
        {
            kb.SetVisibility(visibility.Value);
        }
    }

    public virtual async Task DeleteAsync(
        KnowledgeBase kb,
        CancellationToken cancellationToken = default)
    {
        await _kbRepository.DeleteAsync(kb, autoSave: true, cancellationToken);
    }

    protected virtual async Task<string> MakeUniqueSlugAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        var original = slug;
        var index = 1;

        while (await _kbRepository.FindByCodeAsync(slug, cancellationToken) != null)
        {
            slug = $"{original}-{index++}";
        }

        return slug;
    }
}
