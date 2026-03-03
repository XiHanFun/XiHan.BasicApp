#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:UpgradeAppService
// Guid:0d8c6b6d-1e9f-4c25-9f8b-2a1d8a1b4c8f
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/01 18:21:40
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using XiHan.BasicApp.Upgrade.Application.Dtos;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Application.Services;
using XiHan.Framework.Upgrade.Abstractions;

namespace XiHan.BasicApp.Upgrade.Application.AppServices.Implementations;

/// <summary>
/// 系统升级应用服务
/// </summary>
[DynamicApi(Group = "BasicApp.Upgrade", GroupName = "系统升级服务")]
public class UpgradeAppService : ApplicationServiceBase, IUpgradeAppService
{
    private readonly IUpgradeStatusService _statusService;
    private readonly IUpgradeCoordinator _coordinator;
    private readonly IHttpContextAccessor _httpContextAccessor;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="statusService">升级状态服务</param>
    /// <param name="coordinator">升级协调器</param>
    /// <param name="httpContextAccessor">HTTP上下文访问器</param>
    public UpgradeAppService(
        IUpgradeStatusService statusService,
        IUpgradeCoordinator coordinator,
        IHttpContextAccessor httpContextAccessor)
    {
        _statusService = statusService;
        _coordinator = coordinator;
        _httpContextAccessor = httpContextAccessor;
    }

    /// <summary>
    /// 获取系统版本信息
    /// </summary>
    /// <param name="clientVersion">客户端版本</param>
    /// <returns>系统版本信息</returns>
    [HttpGet]
    public async Task<SystemVersionDto> GetVersionAsync(string? clientVersion = null)
    {
        var cancellationToken = _httpContextAccessor.HttpContext?.RequestAborted ?? CancellationToken.None;
        if (string.IsNullOrWhiteSpace(clientVersion))
        {
            clientVersion = _httpContextAccessor.HttpContext?.Request.Headers["X-Client-Version"].FirstOrDefault();
        }

        var snapshot = await _statusService.GetVersionSnapshotAsync(clientVersion, cancellationToken);
        return new SystemVersionDto
        {
            CurrentAppVersion = snapshot.CurrentAppVersion,
            CurrentDbVersion = snapshot.CurrentDbVersion,
            MinSupportVersion = snapshot.MinSupportVersion,
            RecordedAppVersion = snapshot.RecordedAppVersion,
            NeedUpgrade = snapshot.NeedUpgrade,
            ForceUpgrade = snapshot.ForceUpgrade,
            IsCompatible = snapshot.IsCompatible,
            Status = snapshot.Status,
            IsUpgrading = snapshot.IsUpgrading,
            UpgradeNode = snapshot.UpgradeNode,
            UpgradeStartTime = snapshot.UpgradeStartTime
        };
    }

    /// <summary>
    /// 启动升级
    /// </summary>
    /// <returns></returns>
    [HttpPost]
    public async Task<UpgradeStartResultDto> StartUpgradeAsync()
    {
        var result = await _coordinator.StartAsync();
        return new UpgradeStartResultDto
        {
            Started = result.Started,
            Status = result.Status,
            Message = result.Message
        };
    }

    /// <summary>
    /// 获取升级状态
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    public async Task<UpgradeStatusDto> GetUpgradeStatusAsync()
    {
        var cancellationToken = _httpContextAccessor.HttpContext?.RequestAborted ?? CancellationToken.None;
        var snapshot = await _statusService.GetVersionSnapshotAsync(null, cancellationToken);
        return new UpgradeStatusDto
        {
            Status = snapshot.Status,
            IsUpgrading = snapshot.IsUpgrading,
            UpgradeNode = snapshot.UpgradeNode,
            UpgradeStartTime = snapshot.UpgradeStartTime
        };
    }
}
