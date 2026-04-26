#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IResourceRepository
// Guid:b3554e3d-beb4-4d89-8f57-c7476847a6c4
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 资源仓储接口
/// </summary>
public interface IResourceRepository : ISaasAggregateRepository<SysResource>
{
    /// <summary>
    /// 根据当前租户和资源编码获取资源
    /// </summary>
    Task<SysResource?> GetByCodeAsync(string resourceCode, CancellationToken cancellationToken = default);
}
