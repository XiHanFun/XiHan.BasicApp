#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:WorkflowDefinitionContracts
// Guid:9b25e6c0-73d8-4a41-85f2-c60d19e74b3a
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/17 10:21:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Workflow.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;
using XiHan.Framework.Domain.Shared.Paging.Dtos;

namespace XiHan.BasicApp.Workflow.Application.Contracts;

/// <summary>
/// 工作流定义查询应用服务接口
/// </summary>
public interface IWorkflowDefinitionQueryService : IApplicationService
{
    /// <summary>
    /// 分页查询定义
    /// </summary>
    Task<PageResultDtoBase<WorkflowDefinitionListItemDto>> GetPageAsync(WorkflowDefinitionPageQueryDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取定义详情（含定义 JSON）
    /// </summary>
    Task<WorkflowDefinitionDetailDto?> GetDetailAsync(long id, CancellationToken cancellationToken = default);
}

/// <summary>
/// 工作流定义命令应用服务接口
/// </summary>
public interface IWorkflowDefinitionAppService : IApplicationService
{
    /// <summary>
    /// 创建定义草稿（版本号自动取编码下最大版本 + 1）
    /// </summary>
    Task<WorkflowDefinitionDetailDto> CreateAsync(WorkflowDefinitionCreateDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新草稿定义
    /// </summary>
    Task<WorkflowDefinitionDetailDto> UpdateDraftAsync(WorkflowDefinitionUpdateDraftDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 发布定义（发布前结构校验）
    /// </summary>
    Task<WorkflowDefinitionDetailDto> PublishAsync(WorkflowDefinitionIdDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 基于最新版本创建新草稿版本
    /// </summary>
    Task<WorkflowDefinitionDetailDto> NewVersionAsync(WorkflowDefinitionNewVersionDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 停用定义
    /// </summary>
    Task<WorkflowDefinitionDetailDto> DisableAsync(WorkflowDefinitionIdDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 归档定义
    /// </summary>
    Task<WorkflowDefinitionDetailDto> ArchiveAsync(WorkflowDefinitionIdDto input, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除草稿定义
    /// </summary>
    Task DeleteAsync(long id, CancellationToken cancellationToken = default);
}
