#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:OAuthAppQueryService
// Guid:ea1ef831-85cf-40a4-9340-37959029e7f4
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
/// OAuth 应用查询应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "OAuth应用")]
public sealed class OAuthAppQueryService(IOAuthAppRepository oauthAppRepository)
    : SaasApplicationService, IOAuthAppQueryService
{
    /// <summary>
    /// OAuth 应用仓储
    /// </summary>
    private readonly IOAuthAppRepository _oauthAppRepository = oauthAppRepository;

    /// <summary>
    /// 获取 OAuth 应用分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>OAuth 应用分页列表</returns>
    [PermissionAuthorize(SaasPermissionCodes.OAuthApp.Read)]
    public async Task<PageResultDtoBase<OAuthAppListItemDto>> GetOAuthAppPageAsync(OAuthAppPageQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidatePageInput(input);

        var request = BuildOAuthAppPageRequest(input);
        var apps = await _oauthAppRepository.GetPagedAsync(request, cancellationToken);
        return apps.Map(OAuthAppApplicationMapper.ToListItemDto);
    }

    /// <summary>
    /// 获取 OAuth 应用详情
    /// </summary>
    /// <param name="id">OAuth 应用主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>OAuth 应用详情</returns>
    [PermissionAuthorize(SaasPermissionCodes.OAuthApp.Read)]
    public async Task<OAuthAppDetailDto?> GetOAuthAppDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "OAuth 应用主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var app = await _oauthAppRepository.GetByIdAsync(id, cancellationToken);
        return app is null ? null : OAuthAppApplicationMapper.ToDetailDto(app);
    }

    /// <summary>
    /// 构建 OAuth 应用分页请求
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <returns>OAuth 应用分页请求</returns>
    private static BasicAppPRDto BuildOAuthAppPageRequest(OAuthAppPageQueryDto input)
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
                nameof(SysOAuthApp.AppName),
                nameof(SysOAuthApp.AppDescription),
                nameof(SysOAuthApp.ClientId),
                nameof(SysOAuthApp.Homepage),
                nameof(SysOAuthApp.Remark));
        }

        if (input.AppType.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysOAuthApp.AppType), input.AppType.Value);
        }

        if (input.Status.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysOAuthApp.Status), input.Status.Value);
        }

        if (input.SkipConsent.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysOAuthApp.SkipConsent), input.SkipConsent.Value);
        }

        request.Conditions.AddSort(nameof(SysOAuthApp.Status), SortDirection.Descending, 0);
        request.Conditions.AddSort(nameof(SysOAuthApp.CreatedTime), SortDirection.Descending, 1);
        request.Conditions.AddSort(nameof(SysOAuthApp.AppName), SortDirection.Ascending, 2);
        return request;
    }

    /// <summary>
    /// 校验分页参数
    /// </summary>
    /// <param name="input">查询参数</param>
    private static void ValidatePageInput(OAuthAppPageQueryDto input)
    {
        ValidateMaxLength(input.Keyword, 200, nameof(input.Keyword), "关键字长度不能超过 200。");
        if (input.AppType.HasValue)
        {
            ValidateEnum(input.AppType.Value, nameof(input.AppType));
        }

        if (input.Status.HasValue)
        {
            ValidateEnum(input.Status.Value, nameof(input.Status));
        }
    }

    /// <summary>
    /// 校验枚举值
    /// </summary>
    private static void ValidateEnum<TEnum>(TEnum value, string paramName)
        where TEnum : struct, Enum
    {
        if (!Enum.IsDefined(value))
        {
            throw new ArgumentOutOfRangeException(paramName, value, "枚举值无效。");
        }
    }

    /// <summary>
    /// 校验字符串长度
    /// </summary>
    private static void ValidateMaxLength(string? value, int maxLength, string paramName, string message)
    {
        if (!string.IsNullOrWhiteSpace(value) && value.Trim().Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }
    }
}
