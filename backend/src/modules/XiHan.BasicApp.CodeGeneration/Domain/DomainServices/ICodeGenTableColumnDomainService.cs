#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ICodeGenTableColumnDomainService
// Guid:c0de9e00-0a03-4a00-9000-000000000a03
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/20 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.CodeGeneration.Domain.Entities;

namespace XiHan.BasicApp.CodeGeneration.Domain.DomainServices;

/// <summary>
/// 代码生成列配置领域服务接口
/// </summary>
public interface ICodeGenTableColumnDomainService
{
    /// <summary>
    /// 按表批量保存列配置（覆盖各列的可变字段）
    /// </summary>
    Task<CodeGenTableColumnBatchSaveResult> BatchSaveColumnsAsync(CodeGenTableColumnBatchSaveCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 单列更新列配置
    /// </summary>
    Task<CodeGenTableColumnCommandResult> UpdateColumnAsync(CodeGenTableColumnUpdateCommand command, CancellationToken cancellationToken = default);
}
