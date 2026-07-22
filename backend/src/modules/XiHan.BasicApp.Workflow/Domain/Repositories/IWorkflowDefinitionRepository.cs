// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.BasicApp.Workflow.Domain.Entities;
using XiHan.Framework.Workflow.Abstractions.Definitions;

namespace XiHan.BasicApp.Workflow.Domain.Repositories;

/// <summary>
/// 工作流定义仓储接口
/// </summary>
public interface IWorkflowDefinitionRepository : ISaasRepository<SysWorkflowDefinition>
{
    /// <summary>
    /// 按编码和版本查找定义
    /// </summary>
    /// <param name="code">流程编码</param>
    /// <param name="version">版本号</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>定义实体（不存在返回 null）</returns>
    Task<SysWorkflowDefinition?> GetByCodeAndVersionAsync(string code, int version, CancellationToken cancellationToken = default);

    /// <summary>
    /// 查找编码下最新的已发布定义
    /// </summary>
    /// <param name="code">流程编码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>定义实体（不存在返回 null）</returns>
    Task<SysWorkflowDefinition?> GetLatestPublishedAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取编码下的最大版本号
    /// </summary>
    /// <param name="code">流程编码</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>最大版本号（编码不存在返回 0）</returns>
    Task<int> GetMaxVersionAsync(string code, CancellationToken cancellationToken = default);

    /// <summary>
    /// 查询定义列表（按编码升序、版本降序）
    /// </summary>
    /// <param name="code">流程编码（为空表示不过滤）</param>
    /// <param name="status">状态（为空表示不过滤）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>定义实体列表</returns>
    Task<List<SysWorkflowDefinition>> GetDefinitionListAsync(string? code, WorkflowDefinitionStatus? status, CancellationToken cancellationToken = default);
}
