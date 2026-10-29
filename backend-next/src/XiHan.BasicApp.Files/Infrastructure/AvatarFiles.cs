// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Contracts.Files;
using XiHan.BasicApp.Files.Domain.Services;

namespace XiHan.BasicApp.Files.Infrastructure;

/// <summary>
/// 头像文件的回收：头像固定存平台侧，按平台侧删除（副本软删除，对象不再被引用即删）。
/// </summary>
/// <param name="files">文件服务。</param>
public sealed class AvatarFiles(FileService files) : IAvatarFiles
{
    /// <inheritdoc />
    public async Task ReleaseAsync(Guid fileId, CancellationToken cancellationToken)
    {
        // 已经不在（被管理员删了）即逐项失败，不影响换头像
        await files.DeleteAsync(TenantIds.Platform, [fileId], cancellationToken).ConfigureAwait(false);
    }
}
