// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Settings;
using XiHan.BasicApp.Settings.Domain.Services;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Settings.Infrastructure;

/// <summary>
/// 站点系统名的 <see cref="ISiteBrand"/> 实现
/// </summary>
/// <param name="profiles">站点设置</param>
public sealed class SiteBrand(SiteProfiles profiles) : ISiteBrand
{
    /// <inheritdoc />
    public async Task<Result<string>> NameOfAsync(string owner, CancellationToken cancellationToken)
    {
        var effective = await profiles.EffectiveAsync(owner, cancellationToken).ConfigureAwait(false);
        return effective.TryGetValue(out var settings)
            ? settings.Name ?? AppInfo.Name
            : effective.Error!;
    }
}
