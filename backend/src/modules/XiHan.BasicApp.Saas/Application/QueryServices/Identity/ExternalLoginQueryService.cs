#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ExternalLoginQueryService
// Guid:88c84d7b-964a-4251-b047-c195375b4fa1
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
/// 第三方登录绑定查询应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "第三方登录")]
public sealed class ExternalLoginQueryService
    : SaasApplicationService, IExternalLoginQueryService
{
    /// <summary>
    /// 第三方登录绑定仓储
    /// </summary>
    private readonly IExternalLoginRepository _externalLoginRepository;

    /// <summary>
    /// 用户仓储
    /// </summary>
    private readonly IUserRepository _userRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public ExternalLoginQueryService(
        IExternalLoginRepository externalLoginRepository,
        IUserRepository userRepository)
    {
        _externalLoginRepository = externalLoginRepository;
        _userRepository = userRepository;
    }

    /// <summary>
    /// 获取第三方登录绑定分页列表
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>第三方登录绑定分页列表</returns>
    [PermissionAuthorize(SaasPermissionCodes.ExternalLogin.Read)]
    [HttpPost]
    public async Task<PageResultDtoBase<ExternalLoginListItemDto>> GetExternalLoginPageAsync(ExternalLoginPageQueryDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        ValidatePageInput(input);

        var request = BuildExternalLoginPageRequest(input);
        var externalLoginPage = await _externalLoginRepository.GetPagedAsync(request, cancellationToken);
        if (externalLoginPage.Items.Count == 0)
        {
            return new PageResultDtoBase<ExternalLoginListItemDto>([], externalLoginPage.Page);
        }

        var userMap = await BuildUserMapAsync(externalLoginPage.Items.Select(externalLogin => externalLogin.UserId), cancellationToken);
        var items = externalLoginPage.Items
            .Select(externalLogin => ExternalLoginApplicationMapper.ToListItemDto(
                externalLogin,
                userMap.GetValueOrDefault(externalLogin.UserId)))
            .ToList();

        return new PageResultDtoBase<ExternalLoginListItemDto>(items, externalLoginPage.Page);
    }

    /// <summary>
    /// 获取第三方登录绑定详情
    /// </summary>
    /// <param name="id">第三方登录绑定主键</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>第三方登录绑定详情</returns>
    [PermissionAuthorize(SaasPermissionCodes.ExternalLogin.Read)]
    public async Task<ExternalLoginDetailDto?> GetExternalLoginDetailAsync(long id, CancellationToken cancellationToken = default)
    {
        if (id <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(id), "第三方登录绑定主键必须大于 0。");
        }

        cancellationToken.ThrowIfCancellationRequested();

        var externalLogin = await _externalLoginRepository.GetByIdAsync(id, cancellationToken);
        if (externalLogin is null)
        {
            return null;
        }

        var user = await _userRepository.GetByIdAsync(externalLogin.UserId, cancellationToken);
        return ExternalLoginApplicationMapper.ToDetailDto(externalLogin, user);
    }

    /// <summary>
    /// 构建第三方登录绑定分页请求
    /// </summary>
    /// <param name="input">查询条件</param>
    /// <returns>第三方登录绑定分页请求</returns>
    private static BasicAppPRDto BuildExternalLoginPageRequest(ExternalLoginPageQueryDto input)
    {
        var request = new BasicAppPRDto
        {
            Page = input.Page,
            Conditions = new QueryConditions()
        };

        if (!string.IsNullOrWhiteSpace(input.Keyword))
        {
            request.Conditions.SetKeyword<SysExternalLogin>(
                input.Keyword.Trim(),
                login => login.Provider,
                login => login.ProviderDisplayName);
        }

        if (input.UserId.HasValue)
        {
            request.Conditions.AddFilter((SysExternalLogin login) => login.UserId, input.UserId.Value);
        }

        if (!string.IsNullOrWhiteSpace(input.Provider))
        {
            request.Conditions.AddFilter((SysExternalLogin login) => login.Provider, input.Provider.Trim());
        }

        if (input.LastLoginTimeStart.HasValue)
        {
            request.Conditions.AddFilter((SysExternalLogin login) => login.LastLoginTime, input.LastLoginTimeStart.Value, QueryOperator.GreaterThanOrEqual);
        }

        if (input.LastLoginTimeEnd.HasValue)
        {
            request.Conditions.AddFilter((SysExternalLogin login) => login.LastLoginTime, input.LastLoginTimeEnd.Value, QueryOperator.LessThanOrEqual);
        }

        request.Conditions.AddSort((SysExternalLogin login) => login.LastLoginTime, SortDirection.Descending, 0);
        request.Conditions.AddSort((SysExternalLogin login) => login.CreatedTime, SortDirection.Descending, 1);
        return request;
    }

    /// <summary>
    /// 校验分页参数
    /// </summary>
    /// <param name="input">查询参数</param>
    private static void ValidatePageInput(ExternalLoginPageQueryDto input)
    {
        ValidateOptionalId(input.UserId, nameof(input.UserId), "用户主键必须大于 0。");
        ValidateMaxLength(input.Keyword, 200, nameof(input.Keyword), "关键字长度不能超过 200。");
        ValidateMaxLength(input.Provider, 50, nameof(input.Provider), "提供商名称长度不能超过 50。");
        if (input.LastLoginTimeStart.HasValue &&
            input.LastLoginTimeEnd.HasValue &&
            input.LastLoginTimeStart.Value > input.LastLoginTimeEnd.Value)
        {
            throw new ArgumentOutOfRangeException(nameof(input.LastLoginTimeStart), "最后登录开始时间不能晚于结束时间。");
        }
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
    /// 构建用户映射
    /// </summary>
    /// <param name="userIds">用户主键集合</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户映射</returns>
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
}
