#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IImportHistoryQueryService
// Guid:5d1e7b96-2f8a-4c3d-e5b0-9a6c4f3d2b85
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/12 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 导入历史查询应用服务接口（读侧：当前用户在指定页面的最近导入记录）
/// </summary>
public interface IImportHistoryQueryService : IApplicationService
{
    /// <summary>
    /// 获取当前用户在指定页面最近的导入记录（按创建时间倒序，最多 50 条）
    /// </summary>
    Task<List<ImportHistoryDto>> GetMineAsync(string pageCode, int count = 10, CancellationToken cancellationToken = default);
}
