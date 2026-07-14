#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:EmailConfigQueryService
// Guid:8b3e6d05-2f49-4c71-9a58-4d0f7e2c6b93
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/02 16:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Extensions;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Application.Services;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Domain.Shared.Paging.Dtos;
using XiHan.Framework.Domain.Shared.Paging.Enums;
using XiHan.Framework.Domain.Shared.Paging.Models;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 邮件配置查询应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "邮件配置")]
public sealed class EmailConfigQueryService
    : SaasApplicationService, IEmailConfigQueryService
{
    /// <summary>
    /// 邮件配置仓储
    /// </summary>
    private readonly IEmailConfigRepository _emailConfigRepository;

    /// <summary>
    /// 字段级安全（排序/过滤门控）
    /// </summary>
    private readonly IFieldSecurityService _fieldSecurity;

    /// <summary>
    /// 构造函数
    /// </summary>
    public EmailConfigQueryService(
        IEmailConfigRepository emailConfigRepository,
        IFieldSecurityService fieldSecurityService)
    {
        _emailConfigRepository = emailConfigRepository;
        _fieldSecurity = fieldSecurityService;
    }

    /// <summary>
    /// 获取邮件配置分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>邮件配置分页列表</returns>
    [PermissionAuthorize(SaasPermissionCodes.EmailConfig.Read)]
    [HttpPost]
    public async Task<PageResultDtoBase<EmailConfigListItemDto>> GetEmailConfigPageAsync(EmailConfigPageQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var request = BuildEmailConfigPageRequest(input);

        // 排序：前端选择优先，FLS 门控剔除不可读/已脱敏字段；无有效排序回退默认排序
        await _fieldSecurity.GuardSortsAsync(request.Conditions, "SysEmailConfig", cancellationToken);
        // 过滤：FLS 门控剔除不可读/已脱敏字段
        await _fieldSecurity.GuardFiltersAsync(request.Conditions, "SysEmailConfig", cancellationToken);
        if (request.Conditions.Sorts.Count == 0)
        {
            ApplyEmailConfigSorts(request);
        }

        var configPage = await _emailConfigRepository.GetPagedAsync(request, cancellationToken);
        var items = configPage.Items
            .Select(EmailConfigApplicationMapper.ToListItemDto)
            .ToList();

        return new PageResultDtoBase<EmailConfigListItemDto>(items, configPage.Page);
    }

    /// <summary>
    /// 获取邮件配置详情
    /// </summary>
    /// <param name="id">邮件配置主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>邮件配置详情</returns>
    [PermissionAuthorize(SaasPermissionCodes.EmailConfig.Read)]
    public async Task<EmailConfigDetailDto?> GetEmailConfigDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "邮件配置主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var config = await _emailConfigRepository.GetByIdAsync(id, cancellationToken);
        return config is null ? null : EmailConfigApplicationMapper.ToDetailDto(config);
    }

    /// <summary>
    /// 构建邮件配置分页请求
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <returns>邮件配置分页请求</returns>
    private static BasicAppPRDto BuildEmailConfigPageRequest(EmailConfigPageQueryDto input)
    {
        var request = new BasicAppPRDto
        {
            Page = input.Page,
            Conditions = new QueryConditions()
        };

        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            request.Conditions.SetKeyword<SysEmailConfig>(
                input.Keyword.Trim(),
                config => config.ConfigCode,
                config => config.ConfigName,
                config => config.Remark);
        }

        if (input.IsDefault.HasValue)
        {
            request.Conditions.AddFilter((SysEmailConfig config) => config.IsDefault, input.IsDefault.Value);
        }

        if (input.IsEnabled.HasValue)
        {
            request.Conditions.AddFilter((SysEmailConfig config) => config.IsEnabled, input.IsEnabled.Value);
        }

        // 前端选择的排序原样带入（FLS 门控与默认兜底在调用方 GetEmailConfigPageAsync 处理）
        if (input.Conditions?.Sorts is { Count: > 0 } sorts)
        {
            _ = request.Conditions.AddSorts(sorts);
        }

        // 前端选择的通用过滤原样带入（FLS 门控在调用方处理）
        if (input.Conditions?.Filters is { Count: > 0 } filters)
        {
            _ = request.Conditions.AddFilters(filters);
        }

        return request;
    }

    /// <summary>
    /// 施加邮件配置默认排序（无前端排序时的兜底）
    /// </summary>
    /// <param name="request">分页请求</param>
    private static void ApplyEmailConfigSorts(BasicAppPRDto request)
    {
        request.Conditions.AddSort((SysEmailConfig config) => config.IsDefault, SortDirection.Descending, 0);
        request.Conditions.AddSort((SysEmailConfig config) => config.Sort, SortDirection.Ascending, 1);
        request.Conditions.AddSort((SysEmailConfig config) => config.CreatedTime, SortDirection.Descending, 2);
    }
}
