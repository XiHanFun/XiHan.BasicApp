// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 打印模板仓储契约，所有专用查询都显式接收所属租户以保证作用域精确。
/// </summary>
public interface IPrintTemplateRepository : ISaasRepository<SysPrintTemplate>
{
    /// <summary>
    /// 在明确作用域内按编码查询模板。
    /// </summary>
    /// <param name="ownerTenantId">模板所属租户；0 表示平台全局模板。</param>
    /// <param name="templateCode">模板编码。</param>
    /// <param name="enabledOnly">是否仅返回启用模板。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>匹配模板；不存在时返回 <see langword="null"/>。</returns>
    /// <exception cref="ArgumentException"><paramref name="templateCode"/> 为空。</exception>
    /// <exception cref="OperationCanceledException">查询被取消。</exception>
    Task<SysPrintTemplate?> FindByCodeInScopeAsync(
        long ownerTenantId,
        string templateCode,
        bool enabledOnly,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 在明确作用域内按主键查询模板。
    /// </summary>
    /// <param name="ownerTenantId">模板所属租户；0 表示平台全局模板。</param>
    /// <param name="id">模板主键。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>匹配模板；不存在时返回 <see langword="null"/>。</returns>
    /// <exception cref="OperationCanceledException">查询被取消。</exception>
    Task<SysPrintTemplate?> FindByIdInScopeAsync(long ownerTenantId, long id, CancellationToken cancellationToken = default);
}
