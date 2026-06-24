#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ConfigQueryService
// Guid:5c722adb-2d4d-4235-8c21-864ad4bdc37e
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
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
/// 系统配置查询应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "系统配置")]
public sealed class ConfigQueryService
    : SaasApplicationService, IConfigQueryService
{
    /// <summary>
    /// 系统配置仓储
    /// </summary>
    private readonly IConfigRepository _configRepository;

    /// <summary>
    /// 字段级安全（排序门控）
    /// </summary>
    private readonly IFieldSecurityService _fieldSecurity;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ConfigQueryService(IConfigRepository configRepository, IFieldSecurityService fieldSecurityService)
    {
        _configRepository = configRepository;
        _fieldSecurity = fieldSecurityService;
    }

    /// <summary>
    /// 获取系统配置分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>系统配置分页列表</returns>
    [PermissionAuthorize(SaasPermissionCodes.Config.Read)]
    [HttpPost]
    public async Task<PageResultDtoBase<ConfigListItemDto>> GetConfigPageAsync(ConfigPageQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var request = BuildConfigPageRequest(input);

        // 过滤：前端区间(Between)/多选(In)等条件经 conditions.filters 下发，FLS 门控剔除不可读/已脱敏字段后由框架统一应用
        await _fieldSecurity.GuardFiltersAsync(request.Conditions, "SysConfig", cancellationToken);

        // 排序：前端选择优先，FLS 门控剔除不可读/已脱敏字段；无有效排序回退默认排序
        await _fieldSecurity.GuardSortsAsync(request.Conditions, "SysConfig", cancellationToken);
        if (request.Conditions.Sorts.Count == 0)
        {
            ApplyConfigSorts(request);
        }

        var configPage = await _configRepository.GetPagedAsync(request, cancellationToken);
        if (configPage.Items.Count == 0)
        {
            return new PageResultDtoBase<ConfigListItemDto>([], configPage.Page)
            {
                ExtendDatas = configPage.ExtendDatas
            };
        }

        var items = configPage.Items
            .Select(ConfigApplicationMapper.ToListItemDto)
            .ToList();
        return new PageResultDtoBase<ConfigListItemDto>(items, configPage.Page)
        {
            ExtendDatas = configPage.ExtendDatas
        };
    }

    /// <summary>
    /// 获取系统配置详情
    /// </summary>
    /// <param name="id">系统配置主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>系统配置详情</returns>
    [PermissionAuthorize(SaasPermissionCodes.Config.Read)]
    public async Task<ConfigDetailDto?> GetConfigDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "系统配置主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var config = await _configRepository.GetByIdAsync(id, cancellationToken);
        return config is null ? null : ConfigApplicationMapper.ToDetailDto(config);
    }

    /// <summary>
    /// 构建系统配置分页请求
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <returns>系统配置分页请求</returns>
    private static BasicAppPRDto BuildConfigPageRequest(ConfigPageQueryDto input)
    {
        var request = new BasicAppPRDto
        {
            Page = input.Page,
            Behavior = input.Behavior,
            Conditions = new QueryConditions()
        };

        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            request.Conditions.SetKeyword<SysConfig>(
                input.Keyword.Trim(),
                config => config.ConfigName,
                config => config.ConfigGroup,
                config => config.ConfigKey,
                config => config.ConfigDescription);
        }

        if (input.IsGlobal.HasValue)
        {
            // IsGlobal 为派生属性（TenantId == 0），不落库，故按租户列过滤：全局=租户0，非全局=非租户0
            request.Conditions.AddFilter(
                (SysConfig config) => config.TenantId,
                0L,
                input.IsGlobal.Value ? QueryOperator.Equal : QueryOperator.NotEqual);
        }

        if (!string.IsNullOrWhiteSpace(input.ConfigGroup))
        {
            request.Conditions.AddFilter((SysConfig config) => config.ConfigGroup, input.ConfigGroup.Trim());
        }

        if (input.ConfigType.HasValue)
        {
            request.Conditions.AddFilter((SysConfig config) => config.ConfigType, input.ConfigType.Value);
        }

        if (input.DataType.HasValue)
        {
            request.Conditions.AddFilter((SysConfig config) => config.DataType, input.DataType.Value);
        }

        if (input.IsBuiltIn.HasValue)
        {
            request.Conditions.AddFilter((SysConfig config) => config.IsBuiltIn, input.IsBuiltIn.Value);
        }

        if (input.IsEncrypted.HasValue)
        {
            request.Conditions.AddFilter((SysConfig config) => config.IsEncrypted, input.IsEncrypted.Value);
        }

        if (input.Status.HasValue)
        {
            request.Conditions.AddFilter((SysConfig config) => config.Status, input.Status.Value);
        }

        // 前端选择的排序原样带入（FLS 门控与默认兜底在调用方 GetConfigPageAsync 处理）
        if (input.Conditions?.Sorts is { Count: > 0 } sorts)
        {
            _ = request.Conditions.AddSorts(sorts);
        }

        // 前端区间/多选过滤原样带入（FLS 门控在调用方 GetConfigPageAsync 处理）
        if (input.Conditions?.Filters is { Count: > 0 } filters)
        {
            _ = request.Conditions.AddFilters(filters);
        }
        return request;
    }

    /// <summary>
    /// 应用系统配置默认排序
    /// </summary>
    private static void ApplyConfigSorts(BasicAppPRDto request)
    {
        request.Conditions.AddSort((SysConfig config) => config.ConfigGroup, SortDirection.Ascending, 0);
        request.Conditions.AddSort((SysConfig config) => config.Sort, SortDirection.Ascending, 1);
        request.Conditions.AddSort((SysConfig config) => config.ConfigKey, SortDirection.Ascending, 2);
    }
}
