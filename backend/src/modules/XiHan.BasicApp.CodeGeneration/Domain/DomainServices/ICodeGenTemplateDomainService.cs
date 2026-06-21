#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ICodeGenTemplateDomainService
// Guid:c0de9e00-0b04-4a00-9000-000000000b04
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/20 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.CodeGeneration.Domain.DomainServices;

/// <summary>
/// 代码生成模板领域服务接口
/// </summary>
public interface ICodeGenTemplateDomainService
{
    /// <summary>
    /// 创建模板（模板编码全局唯一）
    /// </summary>
    Task<CodeGenTemplateCommandResult> CreateTemplateAsync(CodeGenTemplateCreateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新模板（编码不可变；内置模板允许改内容）
    /// </summary>
    Task<CodeGenTemplateCommandResult> UpdateTemplateAsync(CodeGenTemplateUpdateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新模板状态
    /// </summary>
    Task<CodeGenTemplateCommandResult> UpdateTemplateStatusAsync(CodeGenTemplateStatusChangeCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除模板（内置模板不允许删除）
    /// </summary>
    Task DeleteTemplateAsync(long id, CancellationToken cancellationToken = default);
}
