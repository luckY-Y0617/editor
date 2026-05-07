using Microsoft.EntityFrameworkCore;
using Northstar.Application.Organizations;
using Northstar.Domain.Organizations;
using Northstar.Domain.Workspaces;
using Northstar.Infrastructure.Persistence;

namespace Northstar.Infrastructure.Organizations;

public sealed class EfOrganizationSettingsRepository : IOrganizationSettingsRepository
{
    private readonly NorthstarDbContext _dbContext;

    public EfOrganizationSettingsRepository(NorthstarDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<bool> OrganizationExistsAsync(Guid organizationId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Organizations
            .AsNoTracking()
            .AnyAsync(organization => organization.Id == organizationId && organization.DeletedAt == null, cancellationToken);
    }

    public async Task<bool> UserCanViewOrganizationAsync(
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await (
            from workspace in _dbContext.Workspaces.AsNoTracking()
            join member in _dbContext.WorkspaceMembers.AsNoTracking() on workspace.Id equals member.WorkspaceId
            where workspace.OrganizationId == organizationId &&
                workspace.DeletedAt == null &&
                member.UserId == userId &&
                member.Status == WorkspaceMemberStatus.Active
            select member.UserId)
            .AnyAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetActiveOrganizationWorkspaceRolesAsync(
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await (
            from workspace in _dbContext.Workspaces.AsNoTracking()
            join member in _dbContext.WorkspaceMembers.AsNoTracking() on workspace.Id equals member.WorkspaceId
            where workspace.OrganizationId == organizationId &&
                workspace.DeletedAt == null &&
                member.UserId == userId &&
                member.Status == WorkspaceMemberStatus.Active
            select member.Role)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task<Organization?> GetOrganizationForUpdateAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Organizations
            .FirstOrDefaultAsync(
                organization => organization.Id == organizationId && organization.DeletedAt == null,
                cancellationToken);
    }

    public async Task<bool> OrganizationSlugExistsAsync(
        string slug,
        Guid exceptOrganizationId,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Organizations
            .AsNoTracking()
            .AnyAsync(
                organization =>
                    organization.Id != exceptOrganizationId &&
                    organization.DeletedAt == null &&
                    organization.Slug == slug,
                cancellationToken);
    }

    public async Task<OrganizationProfileReadModel?> GetProfileAsync(
        Guid organizationId,
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        var organization = await _dbContext.Organizations
            .AsNoTracking()
            .Where(organization => organization.Id == organizationId && organization.DeletedAt == null)
            .Select(organization => new
            {
                organization.Id,
                organization.Name,
                organization.Slug,
                organization.Status,
                organization.CreatedAt,
                organization.UpdatedAt
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (organization is null)
        {
            return null;
        }

        var workspaces = await (
            from workspace in _dbContext.Workspaces.AsNoTracking()
            join member in _dbContext.WorkspaceMembers.AsNoTracking() on workspace.Id equals member.WorkspaceId
            where workspace.OrganizationId == organizationId &&
                workspace.DeletedAt == null &&
                member.UserId == userId &&
                member.Status == WorkspaceMemberStatus.Active
            orderby workspace.Name
            select new OrganizationWorkspaceReadModel(
                workspace.Id,
                workspace.Name,
                workspace.Slug,
                workspace.DefaultSpaceId,
                member.Role,
                workspace.CreatedAt))
            .ToListAsync(cancellationToken);

        return new OrganizationProfileReadModel(
            organization.Id,
            organization.Name,
            organization.Slug,
            organization.Status,
            organization.CreatedAt,
            organization.UpdatedAt,
            workspaces);
    }

    public async Task<IReadOnlyList<OrganizationMemberFlatReadModel>> GetMemberRowsAsync(
        Guid organizationId,
        CancellationToken cancellationToken = default)
    {
        return await (
            from member in _dbContext.WorkspaceMembers.AsNoTracking()
            join user in _dbContext.Users.AsNoTracking() on member.UserId equals user.Id
            join workspace in _dbContext.Workspaces.AsNoTracking() on member.WorkspaceId equals workspace.Id
            where workspace.OrganizationId == organizationId &&
                workspace.DeletedAt == null &&
                user.DeletedAt == null
            orderby user.DisplayName, workspace.Name
            select new OrganizationMemberFlatReadModel(
                user.Id,
                user.Email,
                user.DisplayName,
                workspace.Id.ToString(),
                workspace.Name,
                member.Role,
                member.Status,
                member.JoinedAt))
            .ToListAsync(cancellationToken);
    }
}
