// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Tenancy.Domain.Entities;
using XiHan.BasicApp.Tenancy.Domain.Services;

namespace XiHan.BasicApp.Tenancy.Operations.Editions;

/// <summary>
/// 套餐的响应形态：列表项与详情里的基本信息共用。
/// </summary>
/// <param name="Id">主键。</param>
/// <param name="Code">编码。</param>
/// <param name="Name">名称。</param>
/// <param name="Description">说明。</param>
/// <param name="IsDefault">是否新租户的缺省套餐。</param>
/// <param name="Order">排序号。</param>
/// <param name="PermissionCount">白名单里的权限数。</param>
/// <param name="TenantCount">绑定它的租户数。</param>
/// <param name="CreatedAt">创建时间（UTC）。</param>
public sealed record EditionResponse(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    bool IsDefault,
    int Order,
    int PermissionCount,
    int TenantCount,
    DateTimeOffset CreatedAt)
{
    /// <summary>
    /// 由实体与计数投影。
    /// </summary>
    public static EditionResponse From(SysEdition edition, int permissionCount, int tenantCount)
    {
        ArgumentNullException.ThrowIfNull(edition);
        return new EditionResponse(edition.Id, edition.Code, edition.Name, edition.Description, edition.IsDefault, edition.Order, permissionCount, tenantCount, edition.CreatedAt);
    }
}

/// <summary>
/// <c>editions.list</c> 的响应：全部套餐，按排序号、编码排列。
/// </summary>
/// <param name="Items">套餐。</param>
public sealed record EditionsResponse(IReadOnlyList<EditionResponse> Items);

/// <summary>
/// 套餐详情：<c>editions.get / create / set-permissions</c> 的响应。
/// </summary>
/// <param name="Edition">套餐。</param>
/// <param name="Permissions">白名单（权限码，排序）。</param>
public sealed record EditionDetailResponse(EditionResponse Edition, IReadOnlyList<string> Permissions)
{
    /// <summary>
    /// 读白名单与绑定租户数，组装详情。
    /// </summary>
    public static async Task<EditionDetailResponse> LoadAsync(EditionService service, SysEdition edition, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(edition);
        var permissions = await service.PermissionsOfAsync(edition.Id, cancellationToken).ConfigureAwait(false);
        var tenants = await service.TenantsUsingAsync(edition.Id, cancellationToken).ConfigureAwait(false);
        return new EditionDetailResponse(EditionResponse.From(edition, permissions.Count, tenants.Count), permissions);
    }
}
