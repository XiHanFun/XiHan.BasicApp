#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ICodeGenTableRepository
// Guid:c0de9e00-0102-4a00-9000-000000000102
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/20 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.CodeGeneration.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;

namespace XiHan.BasicApp.CodeGeneration.Domain.Repositories;

/// <summary>
/// 代码生成表配置仓储接口
/// </summary>
public interface ICodeGenTableRepository : ISaasRepository<SysCodeGenTable>
{
    /// <summary>
    /// 根据数据库表名获取
    /// </summary>
    Task<SysCodeGenTable?> GetByTableNameAsync(string tableName, CancellationToken cancellationToken = default);

    /// <summary>
    /// 检查表名是否已配置
    /// </summary>
    Task<bool> ExistsTableNameAsync(string tableName, long? excludeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// 按模块获取表配置
    /// </summary>
    Task<IReadOnlyList<SysCodeGenTable>> GetByModuleAsync(string moduleName, CancellationToken cancellationToken = default);
}
