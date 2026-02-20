#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:LogsController
// Guid:3a1f6be3-c54f-4ea5-aa01-2a0465b834cc
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/02/12 16:39:10
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using XiHan.BasicApp.Rbac.Application.Services.Logs;
using XiHan.Framework.Domain.Shared.Paging.Dtos;

namespace XiHan.BasicApp.WebHost.Controllers;

/// <summary>
/// 日志查询控制器
/// </summary>
[ApiController]
[Authorize]
public class LogsController(LogApplicationService logApplicationService) : ControllerBase
{
    /// <summary>
    /// 分页查询访问日志
    /// </summary>
    [HttpPost("/api/access-logs")]
    public async Task<IActionResult> GetAccessLogsAsync([FromBody] PageRequestDtoBase input, CancellationToken cancellationToken = default)
    {
        var data = await logApplicationService.GetAccessLogsPagedAsync(input, cancellationToken);
        return Ok(new { code = 200, data });
    }

    /// <summary>
    /// 分页查询操作日志
    /// </summary>
    [HttpPost("/api/operation-logs")]
    public async Task<IActionResult> GetOperationLogsAsync([FromBody] PageRequestDtoBase input, CancellationToken cancellationToken = default)
    {
        var data = await logApplicationService.GetOperationLogsPagedAsync(input, cancellationToken);
        return Ok(new { code = 200, data });
    }

    /// <summary>
    /// 分页查询异常日志
    /// </summary>
    [HttpPost("/api/exception-logs")]
    public async Task<IActionResult> GetExceptionLogsAsync([FromBody] PageRequestDtoBase input, CancellationToken cancellationToken = default)
    {
        var data = await logApplicationService.GetExceptionLogsPagedAsync(input, cancellationToken);
        return Ok(new { code = 200, data });
    }

    /// <summary>
    /// 分页查询差异日志
    /// </summary>
    [HttpPost("/api/audit-logs")]
    public async Task<IActionResult> GetAuditLogsAsync([FromBody] PageRequestDtoBase input, CancellationToken cancellationToken = default)
    {
        var data = await logApplicationService.GetAuditLogsPagedAsync(input, cancellationToken);
        return Ok(new { code = 200, data });
    }
}
