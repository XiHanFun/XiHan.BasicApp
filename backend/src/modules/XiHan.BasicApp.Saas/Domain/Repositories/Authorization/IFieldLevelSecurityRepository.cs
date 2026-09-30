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
    /// 取某实体上已启用的规则：当前租户的规则加上平台规则（平台规则对所有租户生效）
    /// </summary>
    /// <param name="entityName">实体名</param>
    /// <param name="cancellationToken">取消令牌</param>
    Task<IReadOnlyList<SysFieldLevelSecurity>> GetEnabledByEntityAsync(string entityName, CancellationToken cancellationToken = default);
}
