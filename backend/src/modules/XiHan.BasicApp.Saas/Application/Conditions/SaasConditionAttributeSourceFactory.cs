// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using XiHan.Framework.Authorization.Conditions;
using XiHan.Framework.MultiTenancy.Abstractions;
using XiHan.Framework.Security.Extensions;
using XiHan.Framework.Security.Users;
using XiHan.Framework.Web.Core.Clients;

namespace XiHan.BasicApp.Saas.Application.Conditions;

/// <summary>
/// 条件属性来源工厂
/// </summary>
public sealed class SaasConditionAttributeSourceFactory : ISaasConditionAttributeSourceFactory
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ICurrentUser _currentUser;
    private readonly ICurrentTenant _currentTenant;
    private readonly SaasConditionSubjectLoader _subjectLoader;
    private readonly IIpRegionResolver _ipRegionResolver;
    private readonly TimeZoneInfo _timeZone;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SaasConditionAttributeSourceFactory(
        IHttpContextAccessor httpContextAccessor,
        ICurrentUser currentUser,
        ICurrentTenant currentTenant,
        SaasConditionSubjectLoader subjectLoader,
        IIpRegionResolver ipRegionResolver,
        IOptions<SaasConditionOptions> options)
    {
        _httpContextAccessor = httpContextAccessor;
        _currentUser = currentUser;
        _currentTenant = currentTenant;
        _subjectLoader = subjectLoader;
        _ipRegionResolver = ipRegionResolver;
        _timeZone = options.Value.ResolveTimeZone();
    }

    /// <inheritdoc />
    public SaasConditionContext CreateCurrentContext(DateTimeOffset now)
    {
        var userId = _currentUser.UserId is > 0 ? _currentUser.UserId : null;
        return new SaasConditionContext
        {
            TenantId = _currentTenant.Id ?? 0,
            Now = now,
            UserId = userId,
            UserName = userId is null ? null : _currentUser.UserName,
            IsImpersonating = userId is not null && _currentUser.IsImpersonating(),
            HttpContext = _httpContextAccessor.HttpContext
        };
    }

    /// <inheritdoc />
    public IConditionAttributeSource Create(SaasConditionContext context)
    {
        ArgumentNullException.ThrowIfNull(context);
        return new MemoizingConditionAttributeSource(new SaasConditionAttributeSource(context, _subjectLoader, _ipRegionResolver, _timeZone));
    }
}
