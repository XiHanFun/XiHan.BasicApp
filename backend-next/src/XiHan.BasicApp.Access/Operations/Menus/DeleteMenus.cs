// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Access.Domain.Services;
using XiHan.BasicApp.Contracts.Batch;
using XiHan.Framework.Kernel.Operations;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Access.Operations.Menus;

/// <summary>
/// 批量删除运营节点。
/// </summary>
/// <remarks>
/// <c>DELETE /api/menus?ids=...</c>（平台侧）。页面节点由页面登记生成，不能删（逐项 <c>ACCESS_CONFLICT_004</c>，要不显示就设为隐藏）；
/// 还有子节点的目录不能删（逐项 <c>ACCESS_CONFLICT_005</c>）。
/// </remarks>
/// <param name="menus">菜单仓储。</param>
/// <param name="service">菜单域服务。</param>
[Operation(Id, Stability = Stability.Preview, Expose = Expose.Http, Permission = AccessPermissions.MenusDelete)]
public sealed class DeleteMenus(IRepository<SysMenu, Guid> menus, MenuService service) : IOperation<MenuIdsRequest, BatchResult>
{
    /// <summary>
    /// 操作 ID。
    /// </summary>
    public const string Id = "menus.delete";

    /// <inheritdoc />
    public async ValueTask<Result<BatchResult>> HandleAsync(MenuIdsRequest request, InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        if (MenuService.RequirePlatform(context) is { } denied)
        {
            return denied;
        }

        var rows = (await service.ListAsync(context.CancellationToken).ConfigureAwait(false)).ToList();
        var succeeded = new List<Guid>();
        var failed = new List<BatchFailure>();
        foreach (var id in request.Ids.Distinct())
        {
            var row = rows.FirstOrDefault(r => r.Id == id);
            if (row is null)
            {
                failed.Add(new BatchFailure(id, AccessErrors.MenuNotFound));
                continue;
            }

            if (row.Kind == MenuKind.Page)
            {
                failed.Add(new BatchFailure(id, AccessErrors.MenuBuiltIn));
                continue;
            }

            if (rows.Any(r => r.ParentId == id))
            {
                failed.Add(new BatchFailure(id, AccessErrors.MenuHasChildren));
                continue;
            }

            var removed = await menus.RemoveAsync(row, context.CancellationToken).ConfigureAwait(false);
            if (removed.IsFailure)
            {
                return removed.Error!;
            }

            rows.Remove(row);
            succeeded.Add(id);
        }

        return new BatchResult(succeeded, failed);
    }
}
