// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Microsoft.AspNetCore.Authorization;
using XiHan.BasicApp.Saas.Application.Dtos.Metadata;
using XiHan.BasicApp.Saas.Application.QueryServices;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Application.Services;

namespace XiHan.BasicApp.Saas.Application.AppServices.Metadata;

/// <summary>
/// 枚举元数据应用服务
/// 只按登录态门控、不挂权限码：枚举标签是所有页面字典的单一事实源，任何登录用户都要能取。
/// </summary>
[Authorize]
[DynamicApi(Group = "BasicApp.Saas", GroupName = "系统SaaS服务", Tag = "枚举元数据")]
public sealed class EnumMetadataAppService : ApplicationServiceBase
{
    private readonly IEnumMetadataQueryService _enumMetadataQueryService;

    /// <summary>
    /// 构造函数
    /// </summary>
    public EnumMetadataAppService(IEnumMetadataQueryService enumMetadataQueryService)
    {
        _enumMetadataQueryService = enumMetadataQueryService;
    }

    /// <summary>
    /// 获取全部枚举元数据
    /// </summary>
    public async Task<List<EnumMetadataDto>> GetAllEnumsAsync()
    {
        return await _enumMetadataQueryService.GetAllEnumsAsync();
    }

    /// <summary>
    /// 获取指定枚举类型的元数据
    /// </summary>
    public async Task<EnumMetadataDto> GetEnumAsync(string enumTypeName)
    {
        return await _enumMetadataQueryService.GetEnumAsync(enumTypeName);
    }
}
