// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Domain.Services;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Seeding;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Access.Seeds;

/// <summary>
/// 预置访问策略种子。
/// </summary>
/// <remarks>
/// 平台数据号段 600 + 模块偏移 10。
/// 按 <see cref="AccessPolicyTemplates"/> 在平台侧写入六条策略（拒绝类停用、只记录类启用，4.x 同）。
/// 运营数据只首写：只补写平台侧还没有的编码，已有的（含改过的）不动。
/// </remarks>
/// <param name="service">策略服务。</param>
public sealed class AccessPolicySeeder(AccessPolicyService service) : ISeeder
{
    /// <inheritdoc />
    public string Name => "access.policies";

    /// <inheritdoc />
    public int Order => SeedOrders.PlatformData + SeedOrders.ModuleOffset;

    /// <inheritdoc />
    public async ValueTask<Result> SeedAsync(InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        foreach (var template in AccessPolicyTemplates.All)
        {
            if ((await service.FindByCodeAsync(TenantIds.Platform, template.Code, context.CancellationToken).ConfigureAwait(false)).HasValue)
            {
                continue;
            }

            var seeded = await service.SeedAsync(TenantIds.Platform, template, context.CancellationToken).ConfigureAwait(false);
            if (seeded.IsFailure)
            {
                return seeded;
            }
        }

        return Result.Success();
    }
}
