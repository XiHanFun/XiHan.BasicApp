// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Access.Domain.Services;
using XiHan.BasicApp.Contracts.Seeding;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Access.Seeds;

/// <summary>
/// 菜单种子
/// </summary>
/// <remarks>
/// 号段 400，目录类每次对齐元数据；规则见 <see cref="MenuAligner"/>
/// </remarks>
/// <param name="aligner">菜单对齐</param>
public sealed class MenuSeeder(MenuAligner aligner) : ISeeder
{
    /// <inheritdoc />
    public string Name => "access.menus";

    /// <inheritdoc />
    public int Order => SeedOrders.Menus;

    /// <inheritdoc />
    public async ValueTask<Result> SeedAsync(InvocationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return await aligner.AlignAsync(context.CancellationToken).ConfigureAwait(false);
    }
}
