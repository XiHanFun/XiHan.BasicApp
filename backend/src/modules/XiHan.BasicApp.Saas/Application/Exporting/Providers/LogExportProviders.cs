// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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
    /// <summary>
    /// 业务类型（= 前端 pageCode）
    /// </summary>
    public override string BusinessType => "log.access";

    /// <summary>
    /// 导出所需权限码（执行器进程内显式校验，补 [PermissionAuthorize] 不触发的缺口）
    /// </summary>
    public override string RequiredPermission => SaasPermissionCodes.AccessLog.Read;

    /// <summary>
    /// 调用对应 QueryService 的分页方法（子类实现）
    /// </summary>
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
    /// <summary>
    /// 业务类型（= 前端 pageCode）
    /// </summary>
    public override string BusinessType => "log.api";

    /// <summary>
    /// 导出所需权限码（执行器进程内显式校验，补 [PermissionAuthorize] 不触发的缺口）
    /// </summary>
    public override string RequiredPermission => SaasPermissionCodes.ApiLog.Read;

    /// <summary>
    /// 调用对应 QueryService 的分页方法（子类实现）
    /// </summary>
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
    /// <summary>
    /// 业务类型（= 前端 pageCode）
    /// </summary>
    public override string BusinessType => "log.login";

    /// <summary>
    /// 导出所需权限码（执行器进程内显式校验，补 [PermissionAuthorize] 不触发的缺口）
    /// </summary>
    public override string RequiredPermission => SaasPermissionCodes.LoginLog.Read;

    /// <summary>
    /// 调用对应 QueryService 的分页方法（子类实现）
    /// </summary>
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
    /// <summary>
    /// 业务类型（= 前端 pageCode）
    /// </summary>
    public override string BusinessType => "log.exception";

    /// <summary>
    /// 导出所需权限码（执行器进程内显式校验，补 [PermissionAuthorize] 不触发的缺口）
    /// </summary>
    public override string RequiredPermission => SaasPermissionCodes.ExceptionLog.Read;

    /// <summary>
    /// 调用对应 QueryService 的分页方法（子类实现）
    /// </summary>
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
    /// <summary>
    /// 业务类型（= 前端 pageCode）
    /// </summary>
    public override string BusinessType => "log.diff";

    /// <summary>
    /// 导出所需权限码（执行器进程内显式校验，补 [PermissionAuthorize] 不触发的缺口）
    /// </summary>
    public override string RequiredPermission => SaasPermissionCodes.DiffLog.Read;

    /// <summary>
    /// 调用对应 QueryService 的分页方法（子类实现）
    /// </summary>
    protected override Task<PageResultDtoBase<DiffLogListItemDto>> QueryPageAsync(DiffLogPageQueryDto query, CancellationToken cancellationToken)
    {
        return service.GetDiffLogPageAsync(query, cancellationToken);
    }
}
