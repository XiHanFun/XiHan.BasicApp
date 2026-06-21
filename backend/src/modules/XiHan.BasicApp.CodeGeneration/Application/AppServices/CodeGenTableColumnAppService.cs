#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:CodeGenTableColumnAppService
// Guid:c0de9e00-0703-4a00-9000-000000000703
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/20 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.CodeGeneration.Application.Contracts;
using XiHan.BasicApp.CodeGeneration.Application.Dtos;
using XiHan.BasicApp.CodeGeneration.Application.Mappers;
using XiHan.BasicApp.CodeGeneration.Domain.DomainServices;
using XiHan.BasicApp.CodeGeneration.Domain.Permissions;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Uow.Attributes;

namespace XiHan.BasicApp.CodeGeneration.Application.AppServices;

/// <summary>
/// 代码生成列配置命令应用服务
/// </summary>
[DynamicApi(Group = "BasicApp.CodeGen", GroupName = "代码生成服务", Tag = "列配置")]
public sealed class CodeGenTableColumnAppService : CodeGenerationApplicationService, ICodeGenTableColumnAppService
{
    private readonly ICodeGenTableColumnDomainService _columnDomainService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public CodeGenTableColumnAppService(ICodeGenTableColumnDomainService columnDomainService)
    {
        _columnDomainService = columnDomainService;
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    [PermissionAuthorize(CodeGenPermissionCodes.Update)]
    public async Task BatchSaveAsync(CodeGenTableColumnBatchSaveDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var command = CodeGenTableColumnApplicationMapper.ToBatchSaveCommand(input);
        await _columnDomainService.BatchSaveColumnsAsync(command, cancellationToken);
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    [PermissionAuthorize(CodeGenPermissionCodes.Update)]
    public async Task<CodeGenTableColumnListItemDto> UpdateAsync(CodeGenTableColumnUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        var command = CodeGenTableColumnApplicationMapper.ToUpdateCommand(input);
        var result = await _columnDomainService.UpdateColumnAsync(command, cancellationToken);
        return CodeGenTableColumnApplicationMapper.ToListItemDto(result.Column);
    }
}
