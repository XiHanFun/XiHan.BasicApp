#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IEnumMetadataQueryService
// Guid:79b8fed7-a188-4d88-9a63-81fdb8ffcbf3
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

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
