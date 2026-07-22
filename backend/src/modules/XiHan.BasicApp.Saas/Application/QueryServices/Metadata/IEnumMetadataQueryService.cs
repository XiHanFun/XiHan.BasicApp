// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Saas.Application.Dtos.Metadata;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 枚举元数据查询服务
/// </summary>
public interface IEnumMetadataQueryService
{
    /// <summary>
    /// 获取全部枚举元数据
    /// </summary>
    Task<List<EnumMetadataDto>> GetAllEnumsAsync();

    /// <summary>
    /// 获取指定枚举类型的元数据
    /// </summary>
    Task<EnumMetadataDto> GetEnumAsync(string enumTypeName);
}
