#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ICodeGenTableColumnRepository
// Guid:c0de9e00-0103-4a00-9000-000000000103
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/20 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.CodeGeneration.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;

namespace XiHan.BasicApp.CodeGeneration.Domain.Repositories;

/// <summary>
/// 代码生成列配置仓储接口
/// </summary>
public interface ICodeGenTableColumnRepository : ISaasRepository<SysCodeGenTableColumn>
{
    /// <summary>
    /// 获取指定表的全部列配置（按 Sort 升序）
    /// </summary>
    Task<IReadOnlyList<SysCodeGenTableColumn>> GetByTableIdAsync(long tableId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除指定表的全部列配置（软删）
    /// </summary>
    Task DeleteByTableIdAsync(long tableId, CancellationToken cancellationToken = default);
}
