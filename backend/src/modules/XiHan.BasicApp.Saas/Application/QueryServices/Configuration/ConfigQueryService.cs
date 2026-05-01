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

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Core.Dtos;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;
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
public sealed class ConfigQueryService(IConfigRepository configRepository)
    : SaasApplicationService, IConfigQueryService
{
    /// <summary>
    /// 系统配置仓储
    /// </summary>
    private readonly IConfigRepository _configRepository = configRepository;

    /// <summary>
    /// 获取系统配置分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>系统配置分页列表</returns>
    [PermissionAuthorize(SaasPermissionCodes.Config.Read)]
    public async Task<PageResultDtoBase<ConfigListItemDto>> GetConfigPageAsync(ConfigPageQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var request = BuildConfigPageRequest(input);
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
            request.Conditions.SetKeyword(
                input.Keyword.Trim(),
                nameof(SysConfig.ConfigName),
                nameof(SysConfig.ConfigGroup),
                nameof(SysConfig.ConfigKey),
                nameof(SysConfig.ConfigDescription));
        }

        if (input.IsGlobal.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysConfig.IsGlobal), input.IsGlobal.Value);
        }

        if (!string.IsNullOrWhiteSpace(input.ConfigGroup))
        {
            request.Conditions.AddFilter(nameof(SysConfig.ConfigGroup), input.ConfigGroup.Trim());
        }

        if (input.ConfigType.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysConfig.ConfigType), input.ConfigType.Value);
        }

        if (input.DataType.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysConfig.DataType), input.DataType.Value);
        }

        if (input.IsBuiltIn.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysConfig.IsBuiltIn), input.IsBuiltIn.Value);
        }

        if (input.IsEncrypted.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysConfig.IsEncrypted), input.IsEncrypted.Value);
        }

        if (input.Status.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysConfig.Status), input.Status.Value);
        }

        request.Conditions.AddSort(nameof(SysConfig.ConfigGroup), SortDirection.Ascending, 0);
        request.Conditions.AddSort(nameof(SysConfig.Sort), SortDirection.Ascending, 1);
        request.Conditions.AddSort(nameof(SysConfig.ConfigKey), SortDirection.Ascending, 2);
        return request;
    }
}
