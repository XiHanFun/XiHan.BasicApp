#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IFieldLevelSecurityRepository
// Guid:b3137631-98b3-4e73-b4e8-28b4d4b2a17b
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/26 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
