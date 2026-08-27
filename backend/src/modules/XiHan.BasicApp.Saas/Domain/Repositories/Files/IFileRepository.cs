// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 文件仓储接口
/// </summary>
public interface IFileRepository : ISaasRepository<SysFile>
{
    /// <summary>
    /// 根据文件哈希获取
    /// </summary>
    Task<SysFile?> GetByHashAsync(string fileHash, CancellationToken cancellationToken = default);

    /// <summary>
    /// 统计指定租户已占用的存储字节数
    /// </summary>
    /// <remarks>
    /// 口径：计入 Normal 与 Uploading 两种状态——上传中的文件已经预占空间，
    /// 漏算会让并发上传绕过配额；软删文件由全局软删过滤器自动排除。
    /// 按 TenantId 精确匹配而非依赖全局租户过滤器：后者放行 TenantId=0 的平台级数据，
    /// 会把平台文件计进任意租户的用量。
    /// </remarks>
    /// <param name="tenantIds">租户主键集合</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>租户主键到已占用字节数的映射；无文件的租户不在结果中</returns>
    Task<IReadOnlyDictionary<long, long>> SumUsedStorageByTenantIdsAsync(IReadOnlyCollection<long> tenantIds, CancellationToken cancellationToken = default);
}
