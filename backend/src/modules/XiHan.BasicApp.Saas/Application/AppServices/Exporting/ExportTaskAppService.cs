#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ExportTaskAppService
// Guid:e4d1f9b7-2c5a-4b0d-96e8-80920314b5c6
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/14 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Authorization;
using System.Text.Json;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.BasicApp.Saas.Infrastructure.Exporting;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Caching.Distributed.Abstracts;
using XiHan.Framework.Security.Users;
using XiHan.Framework.Uow;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 导出任务应用服务（写侧：提交 / 取消 / 删除，均为当前用户自有任务）。
/// 提交仅落 Pending 任务记录；实际拉数/写出/产物由后台 worker 重建发起人上下文后经 QueryService 执行。
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "导出任务")]
public sealed class ExportTaskAppService
    : SaasApplicationService, IExportTaskAppService
{
    private readonly ICurrentUser _currentUser;

    private readonly IExportTaskRepository _repository;

    private readonly IRedisDelayQueue<ExportTaskMessage> _exportTaskQueue;

    private readonly IUnitOfWorkManager _unitOfWorkManager;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ExportTaskAppService(IExportTaskRepository repository, ICurrentUser currentUser, IRedisDelayQueue<ExportTaskMessage> exportTaskQueue, IUnitOfWorkManager unitOfWorkManager)
    {
        _repository = repository;
        _currentUser = currentUser;
        _exportTaskQueue = exportTaskQueue;
        _unitOfWorkManager = unitOfWorkManager;
    }

    /// <inheritdoc />
    public async Task<ExportTaskDto> SubmitAsync(ExportTaskSubmitDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (string.IsNullOrWhiteSpace(input.BusinessType))
        {
            throw new ArgumentException("业务类型不能为空。", nameof(input));
        }
        if (input.Columns is null || input.Columns.Count == 0)
        {
            throw new ArgumentException("导出列不能为空。", nameof(input));
        }

        _ = _currentUser.UserId ?? throw new InvalidOperationException("当前用户未登录。");

        var businessType = input.BusinessType.Trim();
        var taskName = string.IsNullOrWhiteSpace(input.TaskName)
            ? $"{businessType}_{DateTimeOffset.UtcNow:yyyyMMddHHmmss}"
            : input.TaskName.Trim();

        var entity = new SysExportTask
        {
            BusinessType = businessType,
            TaskName = taskName,
            Scope = input.Scope,
            Format = input.Format,
            Status = ExportTaskStatus.Pending,
            Progress = 0,
            QuerySnapshot = string.IsNullOrWhiteSpace(input.QuerySnapshot) ? null : input.QuerySnapshot,
            FieldsSnapshot = JsonSerializer.Serialize(input.Columns)
        };
        // CreatedId（发起人）/ TenantId（发起租户）由审计 AOP 自动注入，后台据此重建上下文
        entity = await _repository.AddAsync(entity, cancellationToken);

        // 提交后入队（延迟 0）：后台导出服务拉取后立即领取执行（替换原 3s 轮询）。无环境 UoW 时直接入队。
        var taskId = entity.BasicId;
        var message = new ExportTaskMessage { ExportTaskId = taskId, CreatedAt = DateTimeOffset.UtcNow };
        var uow = _unitOfWorkManager.Current;
        if (uow is not null)
        {
            uow.OnCompleted(() => _exportTaskQueue.EnqueueAsync(message, TimeSpan.Zero));
        }
        else
        {
            await _exportTaskQueue.EnqueueAsync(message, TimeSpan.Zero, cancellationToken);
        }

        return ToDto(entity);
    }

    /// <inheritdoc />
    public async Task CancelAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "任务主键必须大于 0。");
        }

        var userId = _currentUser.UserId ?? throw new InvalidOperationException("当前用户未登录。");

        var cancelled = await _repository.TryCancelPendingAsync(id, userId, DateTimeOffset.UtcNow, cancellationToken);
        if (!cancelled)
        {
            throw new InvalidOperationException("任务无法取消（不存在、非本人、或已开始执行/已完成）。");
        }
    }

    /// <inheritdoc />
    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "任务主键必须大于 0。");
        }

        var userId = _currentUser.UserId ?? throw new InvalidOperationException("当前用户未登录。");

        var entity = await _repository.GetByIdForUserAsync(id, userId, cancellationToken)
            ?? throw new InvalidOperationException("任务不存在或不属于当前用户。");

        await _repository.DeleteByIdAsync(entity.BasicId, cancellationToken);
    }

    /// <summary>
    /// 实体 → DTO
    /// </summary>
    internal static ExportTaskDto ToDto(SysExportTask entity)
    {
        return new ExportTaskDto
        {
            BasicId = entity.BasicId,
            BusinessType = entity.BusinessType,
            TaskName = entity.TaskName,
            Scope = entity.Scope,
            Format = entity.Format,
            Status = entity.Status,
            Progress = entity.Progress,
            TotalCount = entity.TotalCount,
            ProcessedCount = entity.ProcessedCount,
            FileId = entity.FileId,
            FileName = entity.FileName,
            FileSize = entity.FileSize,
            ErrorMessage = entity.ErrorMessage,
            CreatedTime = entity.CreatedTime,
            StartedTime = entity.StartedTime,
            FinishedTime = entity.FinishedTime
        };
    }
}
