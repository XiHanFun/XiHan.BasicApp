#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:EnumMetadataAppService
// Guid:a7b2f1d6-3c9e-4a08-bf52-8d17e6c4b3a9
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/12 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos.Metadata;
using XiHan.BasicApp.Saas.Application.QueryServices;
using XiHan.Framework.Application.Attributes;
using XiHan.Framework.Application.Services;

namespace XiHan.BasicApp.Saas.Application.AppServices.Metadata;

/// <summary>
/// 枚举元数据应用服务
/// </summary>
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
