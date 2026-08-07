// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Saas.Application.Dtos.Metadata;
using XiHan.BasicApp.Saas.Domain.Numbering;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Application.Services;

namespace XiHan.BasicApp.Saas.Application.AppServices.Metadata;

/// <summary>
/// 时区元数据应用服务
/// 只按登录态门控、不挂权限码：顶栏时区、个人中心时区、编号规则时区共用这一份目录，任何登录用户都要能取。
/// </summary>
/// <remarks>
/// 目录由 <see cref="INumberingFormatter.GetSupportedTimeZones"/> 构建——它筛掉了当前运行环境无法解析的时区，
/// 保证前端选出来的 ID 服务端一定能用。目录本身与编号业务无关，只是构建逻辑目前落在该实现内。
/// </remarks>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "时区元数据")]
public sealed class TimeZoneMetadataAppService : ApplicationServiceBase
{
    private readonly INumberingFormatter _formatter;

    /// <summary>
    /// 构造函数
    /// </summary>
    public TimeZoneMetadataAppService(INumberingFormatter formatter)
    {
        _formatter = formatter;
    }

    /// <summary>
    /// 获取可用时区目录
    /// </summary>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>UTC 优先、其余按基础偏移排序的时区选项</returns>
    public Task<IReadOnlyList<TimeZoneOptionDto>> GetTimeZoneOptionsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<TimeZoneOptionDto> options = [.. _formatter.GetSupportedTimeZones()
            .Select(timeZone => new TimeZoneOptionDto
            {
                Id = timeZone.Id,
                DisplayName = timeZone.DisplayName,
                BaseUtcOffsetMinutes = timeZone.BaseUtcOffsetMinutes,
                SupportsDaylightSavingTime = timeZone.SupportsDaylightSavingTime
            })];
        return Task.FromResult(options);
    }
}
