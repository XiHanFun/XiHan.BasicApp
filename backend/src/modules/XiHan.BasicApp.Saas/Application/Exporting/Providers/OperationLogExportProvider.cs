// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.Framework.Domain.Shared.Paging.Dtos;

namespace XiHan.BasicApp.Saas.Application.Exporting;

/// <summary>
/// 操作日志导出 Provider（业务类型 log.operation，复用 IOperationLogQueryService 分页 + 分表查询）
/// </summary>
public sealed class OperationLogExportProvider : QueryServiceExportProviderBase<OperationLogPageQueryDto, OperationLogListItemDto>
{
    private readonly IOperationLogQueryService _operationLogQueryService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public OperationLogExportProvider(IOperationLogQueryService operationLogQueryService)
    {
        _operationLogQueryService = operationLogQueryService;
    }

    /// <summary>
    /// 业务类型（= 前端 pageCode）
    /// </summary>
    public override string BusinessType => "log.operation";

    /// <summary>
    /// 导出所需权限码（执行器进程内显式校验，补 [PermissionAuthorize] 不触发的缺口）
    /// </summary>
    public override string RequiredPermission => SaasPermissionCodes.OperationLog.Read;

    /// <summary>
    /// 调用对应 QueryService 的分页方法（子类实现）
    /// </summary>
    protected override Task<PageResultDtoBase<OperationLogListItemDto>> QueryPageAsync(OperationLogPageQueryDto query, CancellationToken cancellationToken)
    {
        return _operationLogQueryService.GetOperationLogPageAsync(query, cancellationToken);
    }
}
