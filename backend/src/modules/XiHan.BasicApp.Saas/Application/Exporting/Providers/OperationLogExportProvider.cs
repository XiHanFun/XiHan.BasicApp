#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:OperationLogExportProvider
// Guid:13a0d8f6-1c4e-4f9b-08d2-849506b7c8d9
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
