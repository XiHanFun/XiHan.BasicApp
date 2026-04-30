#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:OAuthCodeQueryService
// Guid:12606922-9670-4e60-9afa-f9dfbd79932f
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
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.BasicApp.Saas.Domain.Repositories;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Domain.Shared.Paging.Dtos;
using XiHan.Framework.Domain.Shared.Paging.Enums;
using XiHan.Framework.Domain.Shared.Paging.Models;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// OAuth 授权码查询应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "OAuth授权码")]
public sealed class OAuthCodeQueryService(
    IOAuthCodeRepository oauthCodeRepository,
    IOAuthAppRepository oauthAppRepository,
    IUserRepository userRepository)
    : SaasApplicationService, IOAuthCodeQueryService
{
    /// <summary>
    /// OAuth 授权码仓储
    /// </summary>
    private readonly IOAuthCodeRepository _oauthCodeRepository = oauthCodeRepository;

    /// <summary>
    /// OAuth 应用仓储
    /// </summary>
    private readonly IOAuthAppRepository _oauthAppRepository = oauthAppRepository;

    /// <summary>
    /// 用户仓储
    /// </summary>
    private readonly IUserRepository _userRepository = userRepository;

    /// <summary>
    /// 获取 OAuth 授权码分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>OAuth 授权码分页列表</returns>
    [PermissionAuthorize(SaasPermissionCodes.OAuthCode.Read)]
    public async Task<PageResultDtoBase<OAuthCodeListItemDto>> GetOAuthCodePageAsync(OAuthCodePageQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidatePageInput(input);

        var request = BuildOAuthCodePageRequest(input, DateTimeOffset.UtcNow);
        var codePage = await _oauthCodeRepository.GetPagedAsync(request, cancellationToken);
        if (codePage.Items.Count == 0)
        {
            return new PageResultDtoBase<OAuthCodeListItemDto>([], codePage.Page);
        }

        var now = DateTimeOffset.UtcNow;
        var appMap = await BuildAppMapAsync(codePage.Items.Select(code => code.ClientId), cancellationToken);
        var userMap = await BuildUserMapAsync(codePage.Items.Select(code => code.UserId), cancellationToken);
        var items = codePage.Items
            .Select(code => OAuthCodeApplicationMapper.ToListItemDto(
                code,
                appMap.GetValueOrDefault(code.ClientId),
                userMap.GetValueOrDefault(code.UserId),
                now))
            .ToList();

        return new PageResultDtoBase<OAuthCodeListItemDto>(items, codePage.Page);
    }

    /// <summary>
    /// 获取 OAuth 授权码详情
    /// </summary>
    /// <param name="id">OAuth 授权码主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>OAuth 授权码详情</returns>
    [PermissionAuthorize(SaasPermissionCodes.OAuthCode.Read)]
    public async Task<OAuthCodeDetailDto?> GetOAuthCodeDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "OAuth 授权码主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var code = await _oauthCodeRepository.GetByIdAsync(id, cancellationToken);
        if (code is null)
        {
            return null;
        }

        var app = await _oauthAppRepository.GetFirstAsync(item => item.ClientId == code.ClientId, cancellationToken);
        var user = await _userRepository.GetByIdAsync(code.UserId, cancellationToken);
        return OAuthCodeApplicationMapper.ToDetailDto(code, app, user, DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// 构建 OAuth 授权码分页请求
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="now">当前时间</param>
    /// <returns>OAuth 授权码分页请求</returns>
    private static BasicAppPRDto BuildOAuthCodePageRequest(OAuthCodePageQueryDto input, DateTimeOffset now)
    {
        var request = new BasicAppPRDto
        {
            Page = input.Page,
            Behavior = input.Behavior,
            Conditions = new QueryConditions()
        };

        if (!string.IsNullOrWhiteSpace(input.ClientId))
        {
            request.Conditions.AddFilter(nameof(SysOAuthCode.ClientId), input.ClientId.Trim());
        }

        if (input.UserId.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysOAuthCode.UserId), input.UserId.Value);
        }

        if (input.IsUsed.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysOAuthCode.IsUsed), input.IsUsed.Value);
        }

        if (input.IsExpired.HasValue)
        {
            request.Conditions.AddFilter(
                nameof(SysOAuthCode.ExpiresTime),
                now,
                input.IsExpired.Value ? QueryOperator.LessThanOrEqual : QueryOperator.GreaterThan);
        }

        if (input.ExpiresTimeStart.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysOAuthCode.ExpiresTime), input.ExpiresTimeStart.Value, QueryOperator.GreaterThanOrEqual);
        }

        if (input.ExpiresTimeEnd.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysOAuthCode.ExpiresTime), input.ExpiresTimeEnd.Value, QueryOperator.LessThanOrEqual);
        }

        if (input.CreatedTimeStart.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysOAuthCode.CreatedTime), input.CreatedTimeStart.Value, QueryOperator.GreaterThanOrEqual);
        }

        if (input.CreatedTimeEnd.HasValue)
        {
            request.Conditions.AddFilter(nameof(SysOAuthCode.CreatedTime), input.CreatedTimeEnd.Value, QueryOperator.LessThanOrEqual);
        }

        request.Conditions.AddSort(nameof(SysOAuthCode.IsUsed), SortDirection.Ascending, 0);
        request.Conditions.AddSort(nameof(SysOAuthCode.ExpiresTime), SortDirection.Descending, 1);
        request.Conditions.AddSort(nameof(SysOAuthCode.CreatedTime), SortDirection.Descending, 2);
        return request;
    }

    /// <summary>
    /// 构建 OAuth 应用映射
    /// </summary>
    private async Task<IReadOnlyDictionary<string, SysOAuthApp>> BuildAppMapAsync(IEnumerable<string> clientIds, CancellationToken cancellationToken)
    {
        var ids = clientIds
            .Where(clientId => !string.IsNullOrWhiteSpace(clientId))
            .Select(clientId => clientId.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (ids.Length == 0)
        {
            return new Dictionary<string, SysOAuthApp>(StringComparer.OrdinalIgnoreCase);
        }

        var apps = await _oauthAppRepository.GetListAsync(
            app => ids.Contains(app.ClientId),
            app => app.AppName,
            cancellationToken);
        return apps.ToDictionary(app => app.ClientId, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// 构建用户映射
    /// </summary>
    private async Task<IReadOnlyDictionary<long, SysUser>> BuildUserMapAsync(IEnumerable<long> userIds, CancellationToken cancellationToken)
    {
        var ids = userIds
            .Where(userId => userId > 0)
            .Distinct()
            .ToArray();

        if (ids.Length == 0)
        {
            return new Dictionary<long, SysUser>();
        }

        var users = await _userRepository.GetByIdsAsync(ids, cancellationToken);
        return users.ToDictionary(user => user.BasicId);
    }

    /// <summary>
    /// 校验分页参数
    /// </summary>
    /// <param name="input">查询参数</param>
    private static void ValidatePageInput(OAuthCodePageQueryDto input)
    {
        ValidateOptionalId(input.UserId, nameof(input.UserId), "用户主键必须大于 0。");
        ValidateMaxLength(input.ClientId, 100, nameof(input.ClientId), "客户端 ID 长度不能超过 100。");
        ValidateRange(input.ExpiresTimeStart, input.ExpiresTimeEnd, nameof(input.ExpiresTimeStart), "过期开始时间不能晚于结束时间。");
        ValidateRange(input.CreatedTimeStart, input.CreatedTimeEnd, nameof(input.CreatedTimeStart), "创建开始时间不能晚于结束时间。");
    }

    /// <summary>
    /// 校验可空主键
    /// </summary>
    private static void ValidateOptionalId(long? id, string paramName, string message)
    {
        if (id is <= 0)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
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

    /// <summary>
    /// 校验时间范围
    /// </summary>
    private static void ValidateRange(DateTimeOffset? start, DateTimeOffset? end, string paramName, string message)
    {
        if (start.HasValue && end.HasValue && start.Value > end.Value)
        {
            throw new ArgumentOutOfRangeException(paramName, message);
        }
    }
}
