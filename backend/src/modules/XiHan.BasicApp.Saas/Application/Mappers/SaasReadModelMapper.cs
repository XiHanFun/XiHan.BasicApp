using Mapster;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Mappers;

/// <summary>
/// SaaS 读模型映射器。
/// </summary>
internal static class SaasReadModelMapper
{
    public static RoleDto MapRole(SysRole entity)
    {
        return entity.Adapt<RoleDto>()!;
    }

    public static PermissionDto MapPermission(SysPermission entity)
    {
        return entity.Adapt<PermissionDto>()!;
    }

    public static DepartmentDto MapDepartment(
        SysDepartment entity,
        IReadOnlyDictionary<long, string>? leaderNameMap = null,
        bool hasChildren = false)
    {
        var dto = entity.Adapt<DepartmentDto>()!;
        dto.HasChildren = hasChildren;

        if (entity.LeaderId.HasValue
            && leaderNameMap is not null
            && leaderNameMap.TryGetValue(entity.LeaderId.Value, out var leaderName))
        {
            dto.LeaderName = leaderName;
        }

        return dto;
    }
}
