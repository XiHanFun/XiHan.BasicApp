#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IFileQueryService
// Guid:6e7f8091-0213-2345-ef01-23456789ab01
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/04/05 06:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Core.Queries;
using XiHan.BasicApp.Saas.Application.Dtos;

namespace XiHan.BasicApp.Saas.Application.QueryServices;

/// <summary>
/// 文件查询服务接口
/// </summary>
public interface IFileQueryService : IQueryService
{
    /// <summary>
    /// 根据 ID 获取文件
    /// </summary>
    Task<FileDto?> GetByIdAsync(long id);
}
