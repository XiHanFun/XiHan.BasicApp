#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PagePreferenceAppService
// Guid:a0612347-8092-4e3f-8b6d-5c701e4f9da6
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/05 00:00:00
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
/// 页面偏好应用服务（按用户 × 页面码存取列设置/视图，跨端同步）。
/// 不解释 Payload 语义，仅作个人偏好载荷的跨端持久化。
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "页面偏好")]
public sealed class PagePreferenceAppService
    : SaasApplicationService, IPagePreferenceAppService
{
    private readonly ICurrentUser _currentUser;

    private readonly IPagePreferenceRepository _repository;

    /// <summary>
    /// 构造函数
    /// </summary>
    public PagePreferenceAppService(IPagePreferenceRepository repository, ICurrentUser currentUser)
    {
        _repository = repository;
        _currentUser = currentUser;
    }

    /// <inheritdoc />
    public async Task<PagePreferenceDto> GetAsync(string pageCode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(pageCode))
        {
            throw new ArgumentException("页面码不能为空。", nameof(pageCode));
        }

        var userId = _currentUser.UserId ?? throw new InvalidOperationException("当前用户未登录。");
        var entity = await _repository.GetByUserAndPageAsync(userId, pageCode, cancellationToken);
        return new PagePreferenceDto { PageCode = pageCode, Payload = entity?.Payload };
    }

    /// <inheritdoc />
    public async Task<PagePreferenceDto> SaveAsync(PagePreferenceSaveDto input, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(input.PageCode))
        {
            throw new ArgumentException("页面码不能为空。", nameof(input));
        }

        var userId = _currentUser.UserId ?? throw new InvalidOperationException("当前用户未登录。");
        var entity = await _repository.GetByUserAndPageAsync(userId, input.PageCode, cancellationToken);
        if (entity is null)
        {
            entity = new SysPagePreference
            {
                UserId = userId,
                PageCode = input.PageCode,
                Payload = input.Payload,
            };
            _ = await _repository.AddAsync(entity, cancellationToken);
        }
        else
        {
            entity.Payload = input.Payload;
            _ = await _repository.UpdateAsync(entity, cancellationToken);
        }

        return new PagePreferenceDto { PageCode = input.PageCode, Payload = input.Payload };
    }
}
