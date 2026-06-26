#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ImportHistoryAppService
// Guid:6e2f8c07-3a9b-4d4e-f6c1-0b7d5a4e3c96
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/12 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Security.Users;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 导入历史应用服务（写侧：导入执行完毕后由前端上报留痕，日志型只写不改）。
/// 仅作行为留痕，导入数据本身经各资源端点逐行创建（权限/校验在各端点落地）。
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "导入历史")]
public sealed class ImportHistoryAppService
    : SaasApplicationService, IImportHistoryAppService
{
    /// <summary>
    /// 错误摘要长度上限（超出截断，保护存储）
    /// </summary>
    private const int ErrorSummaryMaxLength = 8000;

    private readonly ICurrentUser _currentUser;

    private readonly IImportHistoryRepository _repository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ImportHistoryAppService(IImportHistoryRepository repository, ICurrentUser currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    /// <inheritdoc />
    public async Task<ImportHistoryDto> CreateAsync(ImportHistoryCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (string.IsNullOrWhiteSpace(input.PageCode))
        {
            throw new ArgumentException("页面码不能为空。", nameof(input));
        }
        if (string.IsNullOrWhiteSpace(input.FileName))
        {
            throw new ArgumentException("导入文件名不能为空。", nameof(input));
        }

        var userId = _currentUser.UserId ?? throw new InvalidOperationException("当前用户未登录。");

        var errorSummary = input.ErrorSummary;
        if (errorSummary is { Length: > ErrorSummaryMaxLength })
        {
            errorSummary = errorSummary[..ErrorSummaryMaxLength];
        }

        var entity = new SysImportHistory
        {
            UserId = userId,
            PageCode = input.PageCode.Trim(),
            ResourceCode = string.IsNullOrWhiteSpace(input.ResourceCode) ? null : input.ResourceCode.Trim(),
            FileName = input.FileName.Trim(),
            TotalCount = Math.Max(0, input.TotalCount),
            SuccessCount = Math.Max(0, input.SuccessCount),
            FailCount = Math.Max(0, input.FailCount),
            ErrorSummary = errorSummary
        };
        entity = await _repository.AddAsync(entity, cancellationToken);

        return ToDto(entity);
    }

    /// <summary>
    /// 实体 → DTO
    /// </summary>
    internal static ImportHistoryDto ToDto(SysImportHistory entity)
    {
        return new ImportHistoryDto
        {
            BasicId = entity.BasicId,
            PageCode = entity.PageCode,
            ResourceCode = entity.ResourceCode,
            FileName = entity.FileName,
            TotalCount = entity.TotalCount,
            SuccessCount = entity.SuccessCount,
            FailCount = entity.FailCount,
            ErrorSummary = entity.ErrorSummary,
            CreatedTime = entity.CreatedTime
        };
    }
}
