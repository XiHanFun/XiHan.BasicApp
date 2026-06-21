#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ICodeGenTableDomainService
// Guid:c0de9e00-0602-4a00-9000-000000000d02
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/20 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.CodeGeneration.Domain.DomainServices;

/// <summary>
/// 代码生成表配置领域服务接口
/// </summary>
/// <remarks>表配置的创建由"导入数据库表"流程完成，此处仅维护更新/状态/删除（删除级联软删其列配置）。</remarks>
public interface ICodeGenTableDomainService
{
    /// <summary>
    /// 更新表配置（表名唯一，排除自身；load-then-mutate）
    /// </summary>
    Task<CodeGenTableCommandResult> UpdateTableAsync(CodeGenTableUpdateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新表配置状态（仅改 Status/Remark）
    /// </summary>
    Task<CodeGenTableCommandResult> UpdateTableStatusAsync(CodeGenTableStatusChangeCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除表配置（软删表 + 级联软删其列配置）
    /// </summary>
    Task DeleteTableAsync(long id, CancellationToken cancellationToken = default);
}
