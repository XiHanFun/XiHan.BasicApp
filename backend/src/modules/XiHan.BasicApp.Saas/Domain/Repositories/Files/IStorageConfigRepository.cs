#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IStorageConfigRepository
// Guid:e7a4c1d9-3f62-4b85-9a0e-5c8d2f7b1a46
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/12 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 存储配置仓储接口
/// </summary>
public interface IStorageConfigRepository : ISaasRepository<SysStorageConfig>
{
    /// <summary>
    /// 按配置编码查询（租户内唯一）
    /// </summary>
    Task<SysStorageConfig?> GetByCodeAsync(string configCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取默认且启用的存储配置
    /// </summary>
    Task<SysStorageConfig?> GetDefaultAsync(CancellationToken cancellationToken = default);
}
