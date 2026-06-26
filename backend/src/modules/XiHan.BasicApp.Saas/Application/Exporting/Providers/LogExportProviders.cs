#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:LogExportProviders
// Guid:24b1f0a8-3d6c-4e9b-1a05-95c6d7e8f9a0
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/14 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.Framework.Domain.Shared.Paging.Dtos;

namespace XiHan.BasicApp.Saas.Application.Exporting;

/// <summary>
/// 访问日志导出 Provider（log.access）
/// </summary>
public sealed class AccessLogExportProvider(IAccessLogQueryService service)
    : QueryServiceExportProviderBase<AccessLogPageQueryDto, AccessLogListItemDto>
{
    /// <inheritdoc />
    public override string BusinessType => "log.access";

    /// <inheritdoc />
    public override string RequiredPermission => SaasPermissionCodes.AccessLog.Read;

    /// <inheritdoc />
    protected override Task<PageResultDtoBase<AccessLogListItemDto>> QueryPageAsync(AccessLogPageQueryDto query, CancellationToken cancellationToken)
    {
        return service.GetAccessLogPageAsync(query, cancellationToken);
    }
}

/// <summary>
/// 开放接口日志导出 Provider（log.api）
/// </summary>
public sealed class ApiLogExportProvider(IApiLogQueryService service)
    : QueryServiceExportProviderBase<ApiLogPageQueryDto, ApiLogListItemDto>
{
    /// <inheritdoc />
    public override string BusinessType => "log.api";

    /// <inheritdoc />
    public override string RequiredPermission => SaasPermissionCodes.ApiLog.Read;

    /// <inheritdoc />
    protected override Task<PageResultDtoBase<ApiLogListItemDto>> QueryPageAsync(ApiLogPageQueryDto query, CancellationToken cancellationToken)
    {
        return service.GetApiLogPageAsync(query, cancellationToken);
    }
}

/// <summary>
/// 登录日志导出 Provider（log.login）
/// </summary>
public sealed class LoginLogExportProvider(ILoginLogQueryService service)
    : QueryServiceExportProviderBase<LoginLogPageQueryDto, LoginLogListItemDto>
{
    /// <inheritdoc />
    public override string BusinessType => "log.login";

    /// <inheritdoc />
    public override string RequiredPermission => SaasPermissionCodes.LoginLog.Read;

    /// <inheritdoc />
    protected override Task<PageResultDtoBase<LoginLogListItemDto>> QueryPageAsync(LoginLogPageQueryDto query, CancellationToken cancellationToken)
    {
        return service.GetLoginLogPageAsync(query, cancellationToken);
    }
}

/// <summary>
/// 异常日志导出 Provider（log.exception）
/// </summary>
public sealed class ExceptionLogExportProvider(IExceptionLogQueryService service)
    : QueryServiceExportProviderBase<ExceptionLogPageQueryDto, ExceptionLogListItemDto>
{
    /// <inheritdoc />
    public override string BusinessType => "log.exception";

    /// <inheritdoc />
    public override string RequiredPermission => SaasPermissionCodes.ExceptionLog.Read;

    /// <inheritdoc />
    protected override Task<PageResultDtoBase<ExceptionLogListItemDto>> QueryPageAsync(ExceptionLogPageQueryDto query, CancellationToken cancellationToken)
    {
        return service.GetExceptionLogPageAsync(query, cancellationToken);
    }
}

/// <summary>
/// 数据变更日志导出 Provider（log.diff）
/// </summary>
public sealed class DiffLogExportProvider(IDiffLogQueryService service)
    : QueryServiceExportProviderBase<DiffLogPageQueryDto, DiffLogListItemDto>
{
    /// <inheritdoc />
    public override string BusinessType => "log.diff";

    /// <inheritdoc />
    public override string RequiredPermission => SaasPermissionCodes.DiffLog.Read;

    /// <inheritdoc />
    protected override Task<PageResultDtoBase<DiffLogListItemDto>> QueryPageAsync(DiffLogPageQueryDto query, CancellationToken cancellationToken)
    {
        return service.GetDiffLogPageAsync(query, cancellationToken);
    }
}
