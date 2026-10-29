// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Contracts.Files;
using XiHan.BasicApp.Identity.Domain.Entities;
using XiHan.BasicApp.Identity.Operations.Profile;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Identity.Domain.Services;

/// <summary>
/// 换上新传的或去掉本人的头像。
/// </summary>
/// <remarks>
/// 旧的头像文件随之回收（经 Contracts 的 <see cref="IAvatarFiles"/>，没装 Files 即不回收）。
/// 上传在宿主的头像端点里做（校验文件头、存平台侧公开级别），这里只记引用。
/// </remarks>
/// <param name="users">用户仓储。</param>
/// <param name="accounts">账号域服务。</param>
/// <param name="files">头像文件。</param>
public sealed class ProfileAvatars(IRepository<SysUser, Guid> users, UserAccountService accounts, IEnumerable<IAvatarFiles> files)
{
    private readonly IAvatarFiles? _files = files.FirstOrDefault();

    /// <summary>
    /// 取调用方本人与账号安全。
    /// </summary>
    /// <remarks>
    /// 头像端点上传之前先认人。模仿会话不拒绝，4.x 同。
    /// </remarks>
    /// <param name="context">调用上下文。</param>
    public Task<Result<(SysUser User, SysUserSecurity Security)>> CallerAsync(InvocationContext context)
        => ProfileSubject.ResolveAsync(users, accounts, context, rejectImpersonation: false);

    /// <summary>
    /// 把头像换成 <paramref name="fileId"/>（为空即去掉），回收原来的头像文件。
    /// </summary>
    /// <param name="user">用户。</param>
    /// <param name="fileId">新头像文件。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    public async Task<Result> SetAsync(SysUser user, Guid? fileId, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(user);
        var previous = user.AvatarFileId;
        if (previous == fileId)
        {
            return Result.Success();
        }

        user.AvatarFileId = fileId;
        var updated = await users.UpdateAsync(user, cancellationToken).ConfigureAwait(false);
        if (updated.IsFailure)
        {
            return updated;
        }

        if (previous is { } old && _files is not null)
        {
            await _files.ReleaseAsync(old, cancellationToken).ConfigureAwait(false);
        }

        return Result.Success();
    }
}
