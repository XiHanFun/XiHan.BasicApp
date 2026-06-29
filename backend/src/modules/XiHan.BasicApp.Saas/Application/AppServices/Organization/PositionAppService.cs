#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:PositionAppService
// Guid:d46532af-283f-43b4-471c-253647586071
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/29 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Saas.Application.Contracts;
using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Application.Mappers;
using XiHan.BasicApp.Saas.Domain.DomainServices;
using XiHan.BasicApp.Saas.Domain.Permissions;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Authorization.AspNetCore;
using XiHan.Framework.Uow.Attributes;

namespace XiHan.BasicApp.Saas.Application.AppServices;

/// <summary>
/// 岗位命令应用服务
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "岗位")]
public sealed class PositionAppService
    : SaasApplicationService, IPositionAppService
{
    private readonly IPositionDomainService _positionDomainService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public PositionAppService(IPositionDomainService positionDomainService)
    {
        _positionDomainService = positionDomainService;
    }

    /// <summary>
    /// 创建岗位
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Position.Create)]
    public async Task<PositionDetailDto> CreatePositionAsync(PositionCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _positionDomainService.CreatePositionAsync(PositionApplicationMapper.ToCreateCommand(input), cancellationToken);
        return PositionApplicationMapper.ToDetailDto(result.Position);
    }

    /// <summary>
    /// 删除岗位
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Position.Delete)]
    public async Task DeletePositionAsync(long id, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await _positionDomainService.DeletePositionAsync(id, cancellationToken);
    }

    /// <summary>
    /// 更新岗位
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Position.Update)]
    public async Task<PositionDetailDto> UpdatePositionAsync(PositionUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _positionDomainService.UpdatePositionAsync(PositionApplicationMapper.ToUpdateCommand(input), cancellationToken);
        return PositionApplicationMapper.ToDetailDto(result.Position);
    }

    /// <summary>
    /// 更新岗位状态
    /// </summary>
    [UnitOfWork(true)]
    [PermissionAuthorize(SaasPermissionCodes.Position.Status)]
    public async Task<PositionDetailDto> UpdatePositionStatusAsync(PositionStatusUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await _positionDomainService.UpdatePositionStatusAsync(PositionApplicationMapper.ToStatusCommand(input), cancellationToken);
        return PositionApplicationMapper.ToDetailDto(result.Position);
    }
}
