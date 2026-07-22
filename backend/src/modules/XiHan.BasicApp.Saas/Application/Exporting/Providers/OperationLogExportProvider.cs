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

    /// <inheritdoc />
    public override string BusinessType => "log.operation";

    /// <inheritdoc />
    public override string RequiredPermission => SaasPermissionCodes.OperationLog.Read;

    /// <inheritdoc />
    protected override Task<PageResultDtoBase<OperationLogListItemDto>> QueryPageAsync(OperationLogPageQueryDto query, CancellationToken cancellationToken)
    {
        return _operationLogQueryService.GetOperationLogPageAsync(query, cancellationToken);
    }
}
