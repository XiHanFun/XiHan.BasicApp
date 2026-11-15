// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Files;
using XiHan.BasicApp.Files.Domain.Services;

namespace XiHan.BasicApp.Files.Infrastructure;

/// <summary>
/// 站点图片的回收
/// </summary>
/// <remarks>
/// 站点图片与头像一样存平台侧，回收即软删平台侧的那条文件
/// </remarks>
/// <param name="files">文件域服务</param>
public sealed class SiteAssetFiles(FileService files) : ISiteAssetFiles
{
    /// <inheritdoc />
    public async Task ReleaseAsync(Guid fileId, CancellationToken cancellationToken)
    {
        // 已经不在（被管理员删了）即逐项失败，不影响改设置
        await files.DeleteAsync(TenantIds.Platform, [fileId], cancellationToken).ConfigureAwait(false);
    }
}
