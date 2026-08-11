// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 打印模板领域服务契约，维护编码不变、JSON 结构、启停删除和乐观并发不变量。
/// </summary>
public interface IPrintTemplateDomainService
{
    /// <summary>
    /// 在当前租户上下文中创建打印模板。
    /// </summary>
    /// <param name="command">创建命令。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>创建结果。</returns>
    /// <exception cref="XiHan.Framework.Core.Exceptions.UserFriendlyException">模板参数无效或编码已存在。</exception>
    Task<PrintTemplateCommandResult> CreateAsync(PrintTemplateCreateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新当前作用域中的打印模板。
    /// </summary>
    /// <param name="command">更新命令。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>更新结果。</returns>
    /// <exception cref="XiHan.Framework.Core.Exceptions.UserFriendlyException">模板不存在、参数无效或行版本冲突。</exception>
    Task<PrintTemplateCommandResult> UpdateAsync(PrintTemplateUpdateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 启用或停用当前作用域中的打印模板。
    /// </summary>
    /// <param name="command">状态命令。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <returns>更新结果。</returns>
    /// <exception cref="XiHan.Framework.Core.Exceptions.UserFriendlyException">模板不存在、状态无效或行版本冲突。</exception>
    Task<PrintTemplateCommandResult> UpdateStatusAsync(PrintTemplateStatusChangeCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 软删除已经停用的打印模板。
    /// </summary>
    /// <param name="command">删除命令。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    /// <exception cref="XiHan.Framework.Core.Exceptions.UserFriendlyException">模板不存在、仍在启用或行版本冲突。</exception>
    Task DeleteAsync(PrintTemplateDeleteCommand command, CancellationToken cancellationToken = default);
}
