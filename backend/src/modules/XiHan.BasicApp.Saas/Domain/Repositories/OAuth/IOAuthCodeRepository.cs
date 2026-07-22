// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// OAuth 授权码仓储接口
/// </summary>
public interface IOAuthCodeRepository : ISaasRepository<SysOAuthCode>
{
    /// <summary>
    /// 根据授权码获取
    /// </summary>
    Task<SysOAuthCode?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据授权码跨租户获取（Code 全局唯一；供匿名 /connect/token 无租户上下文场景使用）
    /// </summary>
    Task<SysOAuthCode?> GetByCodeIgnoreTenantAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// 原子消费授权码：仅当未使用时置为已使用，返回是否由本次调用成功翻转（防重放/并发换取）
    /// </summary>
    Task<bool> TryConsumeAsync(long codeId, DateTimeOffset now, CancellationToken cancellationToken = default);

    /// <summary>
    /// 清理过期授权码
    /// </summary>
    Task<int> CleanExpiredAsync(DateTimeOffset now, CancellationToken cancellationToken = default);
}
