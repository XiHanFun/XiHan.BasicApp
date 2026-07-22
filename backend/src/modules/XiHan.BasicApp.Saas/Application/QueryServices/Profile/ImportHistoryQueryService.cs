// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Saas.Application.AppServices;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Security.Users;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 导入历史查询应用服务（读侧：当前用户在指定页面的最近导入记录，供导入对话框「最近导入」展示）。
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "导入历史")]
public sealed class ImportHistoryQueryService
    : SaasApplicationService, IImportHistoryQueryService
{
    /// <summary>
    /// 单次查询条数上限
    /// </summary>
    private const int MaxCount = 50;

    private readonly ICurrentUser _currentUser;

    private readonly IImportHistoryRepository _repository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ImportHistoryQueryService(IImportHistoryRepository repository, ICurrentUser currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    /// <inheritdoc />
    public async Task<List<ImportHistoryDto>> GetMineAsync(string pageCode, int count = 10, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(pageCode))
        {
            throw new ArgumentException("页面码不能为空。", nameof(pageCode));
        }

        var userId = _currentUser.UserId ?? throw new InvalidOperationException("当前用户未登录。");
        var normalizedCount = Math.Clamp(count, 1, MaxCount);

        var entities = await _repository.GetRecentByUserAsync(userId, pageCode.Trim(), normalizedCount, cancellationToken);
        return [.. entities.Select(ImportHistoryAppService.ToDto)];
    }
}
