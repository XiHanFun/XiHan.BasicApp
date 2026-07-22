// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

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
/// 代码生成表配置命令应用服务
/// </summary>
/// <remarks>表配置的创建由"导入数据库表"流程完成；此处仅维护更新/状态/删除。</remarks>
[DynamicApi(Group = "BasicApp.CodeGen", GroupName = "代码生成服务", Tag = "表配置")]
public sealed class CodeGenTableAppService : CodeGenerationApplicationService, ICodeGenTableAppService
{
    private readonly ICodeGenTableDomainService _tableDomainService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public CodeGenTableAppService(ICodeGenTableDomainService tableDomainService)
    {
        _tableDomainService = tableDomainService;
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    [PermissionAuthorize(CodeGenPermissionCodes.Update)]
    public async Task<CodeGenTableDetailDto> UpdateAsync(CodeGenTableUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _tableDomainService.UpdateTableAsync(CodeGenTableApplicationMapper.ToUpdateCommand(input), cancellationToken);
        return CodeGenTableApplicationMapper.ToDetailDto(result.Table);
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    [PermissionAuthorize(CodeGenPermissionCodes.Update)]
    public async Task<CodeGenTableDetailDto> UpdateStatusAsync(CodeGenTableStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _tableDomainService.UpdateTableStatusAsync(CodeGenTableApplicationMapper.ToStatusCommand(input), cancellationToken);
        return CodeGenTableApplicationMapper.ToDetailDto(result.Table);
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    [PermissionAuthorize(CodeGenPermissionCodes.Delete)]
    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _tableDomainService.DeleteTableAsync(id, cancellationToken);
    }
}
