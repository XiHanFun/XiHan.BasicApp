// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 消息模板领域服务
/// </summary>
public interface IMessageTemplateDomainService
{
    /// <summary>
    /// 创建消息模板
    /// </summary>
    Task<MessageTemplateCommandResult> CreateAsync(MessageTemplateCreateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新消息模板
    /// </summary>
    Task<MessageTemplateCommandResult> UpdateAsync(MessageTemplateUpdateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新消息模板状态
    /// </summary>
    Task<MessageTemplateCommandResult> UpdateStatusAsync(MessageTemplateStatusChangeCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除消息模板（软删）
    /// </summary>
    Task DeleteAsync(long id, CancellationToken cancellationToken = default);
}
