#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IPasswordHistoryDomainService
// Guid:7f2c5a18-6b4d-4e9a-9c33-1d2e3f4a5b6c
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 密码历史领域服务：在密码变更路径上拦截"重用旧密码"并记录历史
/// </summary>
/// <remarks>
/// 注意：框架 <c>IPasswordHasher</c> 采用 PBKDF2 加盐哈希，同一明文每次哈希结果不同，
/// 因此不能直接比较哈希字符串，必须用 <c>VerifyPassword(历史哈希, 新明文)</c> 逐条比对。
/// </remarks>
public interface IPasswordHistoryDomainService
{
    /// <summary>
    /// 校验新密码未与最近 N 次历史密码重复，重复则抛出 <see cref="InvalidOperationException"/>
    /// </summary>
    /// <param name="userId">用户标识</param>
    /// <param name="newPlainPassword">新密码明文</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task EnsureNotReusedAsync(long userId, string newPlainPassword, CancellationToken cancellationToken = default);

    /// <summary>
    /// 记录一条密码历史（密码变更成功后写入新密码哈希）
    /// </summary>
    /// <param name="userId">用户标识</param>
    /// <param name="newPasswordHash">新密码哈希</param>
    /// <param name="changedTime">变更时间</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task RecordAsync(long userId, string newPasswordHash, DateTimeOffset changedTime, CancellationToken cancellationToken = default);
}
