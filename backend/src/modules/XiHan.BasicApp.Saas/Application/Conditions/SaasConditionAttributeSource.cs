// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using XiHan.BasicApp.Saas.Domain.Conditions;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Authorization.Conditions;
using XiHan.Framework.Security.Claims;
using XiHan.Framework.Web.Api.Security.OpenApi;
using XiHan.Framework.Web.Core.Clients;

namespace XiHan.BasicApp.Saas.Application.Conditions;

/// <summary>
/// 条件属性来源：按 <see cref="SaasConditionAttributes"/> 的登记从取值上下文里取值
/// </summary>
/// <remarks>
/// 惰性取值，只有被条件引用到的属性才去读；要查库的主体属性按组读一次（任职的部门与岗位一起读）。
/// 取不到的一律返回 <see cref="ConditionAttributeValue.Missing"/>，由求值器按「无法判定」处理。
/// </remarks>
public sealed class SaasConditionAttributeSource : IConditionAttributeSource
{
    private const string RoutePrefix = "resource.route.";
    private const string QueryPrefix = "resource.query.";

    private static readonly Dictionary<string, Func<SaasConditionAttributeSource, CancellationToken, ValueTask<ConditionAttributeValue>>> Readers =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [SaasConditionAttributes.SubjectUserId] = static (source, _) => Ready(source._context.UserId?.ToString(CultureInfo.InvariantCulture)),
            [SaasConditionAttributes.SubjectUserName] = static (source, _) => Ready(source._context.UserName),
            [SaasConditionAttributes.SubjectIsImpersonating] = static (source, _) => Ready(source._context.UserId is > 0 ? source._context.IsImpersonating : null),
            [SaasConditionAttributes.SubjectTenantCode] = static (source, token) => source.GetTenantCodeAsync(token),
            [SaasConditionAttributes.SubjectRoleCodes] = static (source, token) => source.GetRoleCodesAsync(token),
            [SaasConditionAttributes.SubjectDepartmentCodes] = static (source, token) => source.GetOrganizationValueAsync(static organization => organization.DepartmentCodes, token),
            [SaasConditionAttributes.SubjectMainDepartmentCode] = static (source, token) => source.GetOrganizationValueAsync(static organization => organization.MainDepartmentCode, token),
            [SaasConditionAttributes.SubjectPositionCodes] = static (source, token) => source.GetOrganizationValueAsync(static organization => organization.PositionCodes, token),
            [SaasConditionAttributes.SubjectMemberType] = static (source, token) => source.GetMemberTypeAsync(token),
            [SaasConditionAttributes.ResourcePermissionCodes] = static (source, _) => Ready(source.GetPermissionCodes()),
            [SaasConditionAttributes.ResourcePath] = static (source, _) => Ready(source._context.HttpContext?.Request.Path.Value),
            [SaasConditionAttributes.ResourceMethod] = static (source, _) => Ready(source._context.HttpContext?.Request.Method.ToUpperInvariant()),
            [SaasConditionAttributes.ResourceService] = static (source, _) => Ready(source.GetActionDescriptor()?.ControllerName),
            [SaasConditionAttributes.ResourceAction] = static (source, _) => Ready(source.GetActionDescriptor()?.ActionName),
            [SaasConditionAttributes.EnvironmentIp] = static (source, _) => Ready(source.GetIpAddress()),
            [SaasConditionAttributes.EnvironmentGeoCountry] = static (source, _) => Ready(source.GetRegion()?.Country),
            [SaasConditionAttributes.EnvironmentGeoProvince] = static (source, _) => Ready(source.GetRegion()?.Province),
            [SaasConditionAttributes.EnvironmentGeoCity] = static (source, _) => Ready(source.GetRegion()?.City),
            [SaasConditionAttributes.EnvironmentGeoIsp] = static (source, _) => Ready(source.GetRegion()?.Isp),
            [SaasConditionAttributes.EnvironmentLocation] = static (source, _) => Ready(source.GetLocation()),
            [SaasConditionAttributes.EnvironmentTime] = static (source, _) => Ready(new TimeOnly(source.LocalNow.Hour, source.LocalNow.Minute)),
            [SaasConditionAttributes.EnvironmentHour] = static (source, _) => Ready(source.LocalNow.Hour),
            // 周一为 1、周日为 7
            [SaasConditionAttributes.EnvironmentWeekday] = static (source, _) => Ready(((int)source.LocalNow.DayOfWeek + 6) % 7 + 1),
            [SaasConditionAttributes.EnvironmentDate] = static (source, _) => Ready(DateOnly.FromDateTime(source.LocalNow.DateTime)),
            [SaasConditionAttributes.EnvironmentClientUserAgent] = static (source, _) => Ready(source._context.HttpContext?.Request.Headers.UserAgent.ToString()),
            [SaasConditionAttributes.EnvironmentClientKind] = static (source, _) => Ready(source.GetClient()?.Kind),
            [SaasConditionAttributes.EnvironmentClientAppId] = static (source, _) => Ready(source.GetClient()?.AppId)
        };

    private readonly SaasConditionContext _context;
    private readonly SaasConditionSubjectLoader _subjectLoader;
    private readonly IIpRegionResolver _ipRegionResolver;

    private SaasConditionOrganization? _organization;
    private bool _regionResolved;
    private IpRegion? _region;

    /// <summary>
    /// 构造函数
    /// </summary>
    public SaasConditionAttributeSource(
        SaasConditionContext context,
        SaasConditionSubjectLoader subjectLoader,
        IIpRegionResolver ipRegionResolver,
        TimeZoneInfo timeZone)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(subjectLoader);
        ArgumentNullException.ThrowIfNull(ipRegionResolver);
        ArgumentNullException.ThrowIfNull(timeZone);

        _context = context;
        _subjectLoader = subjectLoader;
        _ipRegionResolver = ipRegionResolver;
        LocalNow = TimeZoneInfo.ConvertTime(context.Now, timeZone);
    }

    private DateTimeOffset LocalNow { get; }

    /// <inheritdoc />
    public ValueTask<ConditionAttributeValue> GetAsync(string canonicalName, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(canonicalName);
        cancellationToken.ThrowIfCancellationRequested();

        var name = canonicalName.Trim();
        if (Readers.TryGetValue(name, out var reader))
        {
            return reader(this, cancellationToken);
        }

        if (name.Length > RoutePrefix.Length && name.StartsWith(RoutePrefix, StringComparison.OrdinalIgnoreCase))
        {
            return Ready(_context.HttpContext?.Request.RouteValues[name[RoutePrefix.Length..]] is { } routeValue
                ? Convert.ToString(routeValue, CultureInfo.InvariantCulture)
                : null);
        }

        if (name.Length > QueryPrefix.Length && name.StartsWith(QueryPrefix, StringComparison.OrdinalIgnoreCase))
        {
            // 同名多值只取第一个，与标量参数的模型绑定取值一致
            var values = _context.HttpContext?.Request.Query[name[QueryPrefix.Length..]];
            return Ready(values is { Count: > 0 } ? values.Value[0] : null);
        }

        return ValueTask.FromResult(ConditionAttributeValue.Missing);
    }

    private static ValueTask<ConditionAttributeValue> Ready(object? value)
    {
        return ValueTask.FromResult(ToValue(value));
    }

    private static ConditionAttributeValue ToValue(object? value)
    {
        return value is string text && string.IsNullOrWhiteSpace(text)
            ? ConditionAttributeValue.Missing
            : ConditionAttributeValue.Of(value);
    }

    private async ValueTask<ConditionAttributeValue> GetRoleCodesAsync(CancellationToken cancellationToken)
    {
        if (_context.UserId is not > 0)
        {
            return ConditionAttributeValue.Missing;
        }

        var roles = await _subjectLoader.GetRoleCodesAsync(_context.UserId.Value, _context.TenantId, _context.Now, cancellationToken);
        return ConditionAttributeValue.Of(roles);
    }

    private async ValueTask<ConditionAttributeValue> GetOrganizationValueAsync(Func<SaasConditionOrganization, object?> select, CancellationToken cancellationToken)
    {
        if (_context.UserId is not > 0)
        {
            return ConditionAttributeValue.Missing;
        }

        _organization ??= await _subjectLoader.GetOrganizationAsync(_context.UserId.Value, _context.TenantId, cancellationToken);
        return ToValue(select(_organization));
    }

    private async ValueTask<ConditionAttributeValue> GetMemberTypeAsync(CancellationToken cancellationToken)
    {
        if (_context.UserId is not > 0)
        {
            return ConditionAttributeValue.Missing;
        }

        var memberType = await _subjectLoader.GetMemberTypeAsync(_context.UserId.Value, _context.TenantId, cancellationToken);
        return ToValue(memberType?.ToString());
    }

    private async ValueTask<ConditionAttributeValue> GetTenantCodeAsync(CancellationToken cancellationToken)
    {
        return ToValue(await _subjectLoader.GetTenantCodeAsync(_context.TenantId, cancellationToken));
    }

    private IReadOnlyCollection<string>? GetPermissionCodes()
    {
        if (_context.PermissionCodes is not null)
        {
            return _context.PermissionCodes;
        }

        var endpoint = _context.HttpContext?.GetEndpoint();
        return endpoint is null
            ? null
            : [.. endpoint.Metadata.GetOrderedMetadata<PermissionAuthorizeAttribute>()
                .Select(attribute => attribute.PermissionCode)
                .Distinct(StringComparer.OrdinalIgnoreCase)];
    }

    private ControllerActionDescriptor? GetActionDescriptor()
    {
        return _context.HttpContext?.GetEndpoint()?.Metadata.GetMetadata<ControllerActionDescriptor>();
    }

    private IPAddress? GetIpAddress()
    {
        return _context.RemoteIpAddress ?? _context.HttpContext?.Connection.RemoteIpAddress;
    }

    private IpRegion? GetRegion()
    {
        if (!_regionResolved)
        {
            var address = GetIpAddress();
            _region = address is null ? null : _ipRegionResolver.Resolve(address);
            _regionResolved = true;
        }

        return _region;
    }

    private string? GetLocation()
    {
        var region = GetRegion();
        return region is null ? null : string.Concat(region.Country, region.Province, region.City);
    }

    /// <summary>
    /// 客户端类型：开放接口签名客户端优先，其次令牌里的 client_id（OAuth 应用），再次 session_id（网页登录）
    /// </summary>
    private (string Kind, string? AppId)? GetClient()
    {
        var httpContext = _context.HttpContext;
        if (httpContext is null)
        {
            return null;
        }

        if (httpContext.Items[OpenApiSecurityConstants.SecurityClientContextKey] is OpenApiSecurityClient openApiClient)
        {
            return (SaasConditionClientKinds.OpenApi, openApiClient.AccessKey);
        }

        var user = httpContext.User;
        if (user.FindFirst(XiHanClaimTypes.ClientId)?.Value is { Length: > 0 } clientId)
        {
            return (SaasConditionClientKinds.OAuth, clientId);
        }

        return user.FindFirst(XiHanClaimTypes.SessionId)?.Value is { Length: > 0 }
            ? (SaasConditionClientKinds.Web, null)
            : (SaasConditionClientKinds.Anonymous, null);
    }
}
