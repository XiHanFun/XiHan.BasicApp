// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Security;
using XiHan.BasicApp.Contracts.Seeding;
using XiHan.BasicApp.Tenancy.Domain.Entities;
using XiHan.BasicApp.Tenancy.Domain.Services;
using XiHan.Framework.Persistence.Abstractions.Identifiers;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Tenancy.Seeds;

/// <summary>
/// 套餐种子（阶段 <see cref="SeedOrders.Editions"/>，沿用 4.x 的四档）：免费版（新租户缺省）、基础版、专业版、企业版及各自的白名单。
/// 运营数据只首写：编码已存在的套餐（含运营改过或自建的同码套餐）整个跳过，之后新增的权限进哪些套餐由运营在套餐页授予。
/// 白名单按规则从租户能生效的权限目录里取（Tenancy 不引用后面的包，不能写它们的权限常量）：
/// 免费版只读（动作 <c>read</c>）；基础版再加身份与组织两包的管理（不含模仿登录）；专业版再加授权包；企业版是创建时的全部。
/// </summary>
/// <param name="editions">套餐仓储。</param>
/// <param name="service">套餐域服务。</param>
/// <param name="ids">标识生成器。</param>
public sealed class EditionSeeder(IRepository<SysEdition, Guid> editions, EditionService service, IIdGenerator<Guid> ids) : ISeeder
{
    /// <summary>
    /// 免费版编码（新租户缺省）。
    /// </summary>
    public const string Free = "free";

    /// <summary>
    /// 基础版编码。
    /// </summary>
    public const string Basic = "basic";

    /// <summary>
    /// 专业版编码。
    /// </summary>
    public const string Pro = "pro";

    /// <summary>
    /// 企业版编码。
    /// </summary>
    public const string Enterprise = "enterprise";

    /// <inheritdoc />
    public string Name => "tenancy.editions";

    /// <inheritdoc />
    public int Order => SeedOrders.Editions;

    /// <inheritdoc />
    public async ValueTask<Result> SeedAsync(InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        var catalog = service.TenantEffective;
        bool IsRead(PermissionDescriptor p) => string.Equals(p.Action, "read", StringComparison.Ordinal);
        bool IsBasic(PermissionDescriptor p) => IsRead(p) || ((p.Package is "identity" or "organization") && !string.Equals(p.Resource, "impersonation", StringComparison.Ordinal));
        bool IsPro(PermissionDescriptor p) => IsBasic(p) || string.Equals(p.Package, "access", StringComparison.Ordinal);

        (string Code, string Name, string Description, bool IsDefault, int Order, Func<PermissionDescriptor, bool> Includes)[] seeds =
        [
            (Free, "Free", "Read-only access to members, organization and roles.", true, 10, IsRead),
            (Basic, "Basic", "Free plus day-to-day management of users and the organization.", false, 20, IsBasic),
            (Pro, "Professional", "Basic plus role management, grants and assignments.", false, 30, IsPro),
            (Enterprise, "Enterprise", "Every tenant-side permission available when the edition was created.", false, 40, static _ => true),
        ];

        foreach (var seed in seeds)
        {
            if ((await service.FindByCodeAsync(seed.Code, context.CancellationToken).ConfigureAwait(false)).HasValue)
            {
                continue;
            }

            var edition = new SysEdition { Id = ids.NewId(), Code = seed.Code, Name = seed.Name, Description = seed.Description, IsDefault = seed.IsDefault, Order = seed.Order };
            var added = await editions.AddAsync(edition, context.CancellationToken).ConfigureAwait(false);
            if (added.IsFailure)
            {
                return added;
            }

            var replaced = await service.ReplacePermissionsAsync(edition.Id, catalog.Where(seed.Includes).Select(static p => p.Code).ToList(), context.CancellationToken).ConfigureAwait(false);
            if (replaced.IsFailure)
            {
                return replaced;
            }

            if (edition.IsDefault)
            {
                var cleared = await service.ClearOtherDefaultsAsync(edition.Id, context.CancellationToken).ConfigureAwait(false);
                if (cleared.IsFailure)
                {
                    return cleared;
                }
            }
        }

        return Result.Success();
    }
}
