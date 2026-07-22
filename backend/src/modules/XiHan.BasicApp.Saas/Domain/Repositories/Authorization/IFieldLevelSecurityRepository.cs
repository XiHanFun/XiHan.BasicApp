// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Domain.Repositories;

/// <summary>
/// 字段级安全仓储接口
/// </summary>
public interface IFieldLevelSecurityRepository : ISaasRepository<SysFieldLevelSecurity>
{
    /// <summary>
    /// 根据资源和角色获取字段级安全规则
    /// </summary>
    Task<IReadOnlyList<SysFieldLevelSecurity>> GetByResourceAndRoleAsync(long resourceId, long roleId, CancellationToken cancellationToken = default);
}
