// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Access.Domain.Entities;
using XiHan.BasicApp.Contracts;
using XiHan.BasicApp.Organization.Domain.Entities;
using XiHan.Framework.Domain.Specifications;
using XiHan.Framework.Persistence.Abstractions.Queries;
using XiHan.Framework.Persistence.Abstractions.Repositories;
using XiHan.Framework.Persistence.Abstractions.UnitOfWork;
using XiHan.Framework.Primitives;
using XiHan.Framework.Security.Abstractions.Conditions;
using XiHan.Framework.Security.Abstractions.Network;

namespace XiHan.BasicApp.Access.Infrastructure;

/// <summary>
/// 一次判定所用的请求事实（调用时取自调用上下文，试算时由管理员给出）。
/// </summary>
/// <param name="Owner">所属侧（平台侧为 <c>platform</c>）。</param>
/// <param name="UserId">用户；未登录为空。</param>
/// <param name="UserName">登录名。</param>
/// <param name="IsImpersonating">是否模仿登录。</param>
/// <param name="RoleIds">此刻持有的角色。</param>
/// <param name="PermissionCodes">本次调用要求的权限码。</param>
/// <param name="Path">请求路径。</param>
/// <param name="Method">请求方法。</param>
/// <param name="OperationId">操作 ID。</param>
/// <param name="Ip">客户端地址。</param>
/// <param name="UserAgent">客户端 User-Agent。</param>
/// <param name="ClientKind">客户端类型。</param>
/// <param name="Time">判定时刻。</param>
public sealed record ConditionRequest(
    string Owner,
    Guid? UserId,
    string? UserName,
    bool IsImpersonating,
    IReadOnlyList<Guid> RoleIds,
    IReadOnlyList<string> PermissionCodes,
    string? Path,
    string? Method,
    string? OperationId,
    string? Ip,
    string? UserAgent,
    string? ClientKind,
    DateTimeOffset Time);

/// <summary>
/// 按请求事实给出条件属性（4.x 的 SaasConditionAttributeSource）：主体、资源与环境三族；主体的角色码、部门与岗位按需查库（部门与岗位只在租户侧），
/// 归属地经 <see cref="IIpRegionResolver"/>（宿主没装即取不到）；空白一律算取不到。外面套 <see cref="MemoizingConditionAttributeSource"/>，每个属性一次调用只取一次。
/// </summary>
public sealed class ConditionAttributeSource : IConditionAttributeSource
{
    private readonly ConditionRequest _request;
    private readonly IServiceScopeFactory _scopes;
    private readonly IIpRegionResolver? _regions;
    private readonly TimeZoneInfo _timeZone;
    private Task<(IReadOnlyList<string> Departments, string? Main, IReadOnlyList<string> Positions)>? _organization;
    private Maybe<IpRegion>? _region;

    private ConditionAttributeSource(ConditionRequest request, IServiceScopeFactory scopes, IIpRegionResolver? regions, TimeZoneInfo timeZone)
    {
        _request = request;
        _scopes = scopes;
        _regions = regions;
        _timeZone = timeZone;
    }

    /// <summary>
    /// 为一次判定建属性来源。
    /// </summary>
    public static IConditionAttributeSource Create(ConditionRequest request, IServiceProvider services, AccessOptions options)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(options);
        return new MemoizingConditionAttributeSource(new ConditionAttributeSource(request, services.GetRequiredService<IServiceScopeFactory>(), services.GetService<IIpRegionResolver>(), options.ConditionTimeZone));
    }

    /// <inheritdoc />
    public async ValueTask<ConditionAttributeValue> GetAsync(string canonicalName, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(canonicalName);
        var local = TimeZoneInfo.ConvertTime(_request.Time, _timeZone);
        return canonicalName.ToLowerInvariant() switch
        {
            "subject.userid" => Text(_request.UserId?.ToString("D")),
            "subject.username" => _request.UserId is null ? ConditionAttributeValue.Missing : Text(_request.UserName),
            "subject.tenantcode" => _request.UserId is null ? ConditionAttributeValue.Missing : Text(_request.Owner),
            "subject.rolecodes" => _request.UserId is null ? ConditionAttributeValue.Missing : Many(await RoleCodesAsync(cancellationToken).ConfigureAwait(false)),
            "subject.departmentcodes" => _request.UserId is null ? ConditionAttributeValue.Missing : Many((await OrganizationAsync(cancellationToken).ConfigureAwait(false)).Departments),
            "subject.maindepartmentcode" => _request.UserId is null ? ConditionAttributeValue.Missing : Text((await OrganizationAsync(cancellationToken).ConfigureAwait(false)).Main),
            "subject.positioncodes" => _request.UserId is null ? ConditionAttributeValue.Missing : Many((await OrganizationAsync(cancellationToken).ConfigureAwait(false)).Positions),
            "subject.isimpersonating" => _request.UserId is null ? ConditionAttributeValue.Missing : ConditionAttributeValue.Of(_request.IsImpersonating),
            "resource.permissioncodes" => Many(_request.PermissionCodes),
            "resource.path" => Text(_request.Path),
            "resource.method" => Text(_request.Method?.ToUpperInvariant()),
            "resource.operation" => Text(_request.OperationId),
            "environment.ip" => Text(_request.Ip),
            "environment.geo.country" => Text(Region()?.Country),
            "environment.geo.province" => Text(Region()?.Province),
            "environment.geo.city" => Text(Region()?.City),
            "environment.geo.isp" => Text(Region()?.Isp),
            "environment.time" => ConditionAttributeValue.Of(TimeOnly.FromTimeSpan(local.TimeOfDay)),
            "environment.hour" => ConditionAttributeValue.Of((decimal)local.Hour),
            "environment.weekday" => ConditionAttributeValue.Of((decimal)(local.DayOfWeek == DayOfWeek.Sunday ? 7 : (int)local.DayOfWeek)),
            "environment.date" => ConditionAttributeValue.Of(DateOnly.FromDateTime(local.DateTime)),
            "environment.client.useragent" => Text(_request.UserAgent),
            "environment.client.kind" => Text(_request.ClientKind),
            _ => ConditionAttributeValue.Missing,
        };
    }

    private static ConditionAttributeValue Text(string? value) => string.IsNullOrWhiteSpace(value) ? ConditionAttributeValue.Missing : ConditionAttributeValue.Of(value.Trim());

    private static ConditionAttributeValue Many(IReadOnlyList<string> values)
    {
        var present = values.Where(static v => !string.IsNullOrWhiteSpace(v)).ToList();
        return present.Count == 0 ? ConditionAttributeValue.Missing : ConditionAttributeValue.Of(present);
    }

    private IpRegion? Region()
    {
        if (_region is null)
        {
            _region = _regions is not null && IPAddress.TryParse(_request.Ip, out var address) ? _regions.Resolve(address) : Maybe.None<IpRegion>();
        }

        return _region.Value.TryGetValue(out var region) ? region : null;
    }

    private async Task<IReadOnlyList<string>> RoleCodesAsync(CancellationToken cancellationToken)
    {
        if (_request.RoleIds.Count == 0)
        {
            return [];
        }

        return await ReadAsync(null, async (services, ct) =>
        {
            var rows = await services.GetRequiredService<IRepository<SysRole, Guid>>().FindManyAsync(_request.RoleIds, ct).ConfigureAwait(false);
            return (IReadOnlyList<string>)rows.Select(static r => r.Code).ToList();
        }, cancellationToken).ConfigureAwait(false);
    }

    private Task<(IReadOnlyList<string> Departments, string? Main, IReadOnlyList<string> Positions)> OrganizationAsync(CancellationToken cancellationToken)
        => _organization ??= LoadOrganizationAsync(cancellationToken);

    // 部门与岗位是租户库实体：平台侧没有
    private async Task<(IReadOnlyList<string> Departments, string? Main, IReadOnlyList<string> Positions)> LoadOrganizationAsync(CancellationToken cancellationToken)
    {
        if (_request.UserId is not { } userId || string.Equals(_request.Owner, TenantIds.Platform, StringComparison.Ordinal))
        {
            return ([], null, []);
        }

        return await ReadAsync(_request.Owner, async (services, ct) =>
        {
            var memberships = await services.GetRequiredService<IRepository<SysUserDepartment, Guid>>()
                .ListAsync(EntityQuery.All<SysUserDepartment>().Where(new Specification<SysUserDepartment>(m => m.UserId == userId)), ct)
                .ConfigureAwait(false);
            if (memberships.Count == 0)
            {
                return ((IReadOnlyList<string>)[], (string?)null, (IReadOnlyList<string>)[]);
            }

            var departments = (await services.GetRequiredService<IRepository<SysDepartment, Guid>>().FindManyAsync(memberships.Select(static m => m.DepartmentId).Distinct().ToList(), ct).ConfigureAwait(false))
                .Where(static d => d.Status == OrganizationStatus.Active)
                .ToDictionary(static d => d.Id);
            var positionIds = memberships.Where(static m => m.PositionId.HasValue).Select(static m => m.PositionId!.Value).Distinct().ToList();
            var positions = positionIds.Count == 0
                ? []
                : (await services.GetRequiredService<IRepository<SysPosition, Guid>>().FindManyAsync(positionIds, ct).ConfigureAwait(false)).Where(static p => p.Status == OrganizationStatus.Active).Select(static p => p.Code).ToList();
            var main = memberships.FirstOrDefault(m => m.IsPrimary && departments.ContainsKey(m.DepartmentId)) is { } primary ? departments[primary.DepartmentId].Code : null;
            return ((IReadOnlyList<string>)[.. departments.Values.Select(static d => d.Code)], main, (IReadOnlyList<string>)positions);
        }, cancellationToken).ConfigureAwait(false);
    }

    // 判定在事务阶段之前：查库另开只读工作单元，租户库按所属侧显式指定
    private async Task<T> ReadAsync<T>(string? tenantId, Func<IServiceProvider, CancellationToken, Task<T>> read, CancellationToken cancellationToken)
    {
        await using var scope = _scopes.CreateAsyncScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
        await unitOfWork.BeginAsync(new UnitOfWorkOptions(TenantId: tenantId, IsReadOnly: true), cancellationToken).ConfigureAwait(false);
        try
        {
            var result = await read(scope.ServiceProvider, cancellationToken).ConfigureAwait(false);
            (await unitOfWork.CommitAsync(cancellationToken).ConfigureAwait(false)).ThrowIfFailure();
            return result;
        }
        catch
        {
            if (unitOfWork.State == UnitOfWorkState.Active)
            {
                await unitOfWork.RollbackAsync(CancellationToken.None).ConfigureAwait(false);
            }

            throw;
        }
    }
}
