#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IImportHistoryAppService
// Guid:4c0d6a85-1e7f-4b2c-d4a9-8f5b3e2c1a74
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/12 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.Framework.Application.Contracts.Services;

namespace XiHan.BasicApp.Saas.Application.Contracts;

/// <summary>
/// 导入历史应用服务接口（写侧：导入执行完毕后由前端上报留痕）
/// </summary>
public interface IImportHistoryAppService : IApplicationService
{
    /// <summary>
    /// 新增导入历史（追加留痕，日志型只写不改）
    /// </summary>
    Task<ImportHistoryDto> CreateAsync(ImportHistoryCreateDto input, CancellationToken cancellationToken = default);
}
