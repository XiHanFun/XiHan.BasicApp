#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:CodeGenDataSourceAppService
// Guid:c0de9e00-0701-4a00-9000-000000000701
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
/// 代码生成数据源命令应用服务
/// </summary>
[DynamicApi(Group = "BasicApp.CodeGen", GroupName = "代码生成服务", Tag = "数据源")]
public sealed class CodeGenDataSourceAppService : CodeGenerationApplicationService, ICodeGenDataSourceAppService
{
    private readonly ICodeGenDataSourceDomainService _dataSourceDomainService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public CodeGenDataSourceAppService(ICodeGenDataSourceDomainService dataSourceDomainService)
    {
        _dataSourceDomainService = dataSourceDomainService;
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    [PermissionAuthorize(CodeGenPermissionCodes.Create)]
    public async Task<CodeGenDataSourceDetailDto> CreateAsync(CodeGenDataSourceCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _dataSourceDomainService.CreateDataSourceAsync(
            CodeGenDataSourceApplicationMapper.ToCreateCommand(input), cancellationToken);
        return CodeGenDataSourceApplicationMapper.ToDetailDto(result.DataSource);
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    [PermissionAuthorize(CodeGenPermissionCodes.Update)]
    public async Task<CodeGenDataSourceDetailDto> UpdateAsync(CodeGenDataSourceUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _dataSourceDomainService.UpdateDataSourceAsync(
            CodeGenDataSourceApplicationMapper.ToUpdateCommand(input), cancellationToken);
        return CodeGenDataSourceApplicationMapper.ToDetailDto(result.DataSource);
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    [PermissionAuthorize(CodeGenPermissionCodes.Update)]
    public async Task<CodeGenDataSourceDetailDto> UpdateStatusAsync(CodeGenDataSourceStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _dataSourceDomainService.UpdateDataSourceStatusAsync(
            CodeGenDataSourceApplicationMapper.ToStatusCommand(input), cancellationToken);
        return CodeGenDataSourceApplicationMapper.ToDetailDto(result.DataSource);
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    [PermissionAuthorize(CodeGenPermissionCodes.Delete)]
    public async Task DeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _dataSourceDomainService.DeleteDataSourceAsync(id, cancellationToken);
    }

    /// <inheritdoc />
    [UnitOfWork(true)]
    [PermissionAuthorize(CodeGenPermissionCodes.Read)]
    public async Task<CodeGenConnectionTestResultDto> TestConnectionAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _dataSourceDomainService.TestConnectionAsync(id, cancellationToken);
        return CodeGenDataSourceApplicationMapper.ToConnectionTestResultDto(result);
    }
}
