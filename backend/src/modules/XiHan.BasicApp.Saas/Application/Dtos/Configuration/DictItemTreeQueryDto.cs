#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:DictItemTreeQueryDto
// Guid:99c51d43-65e9-49cd-8662-afa09600d3e2
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Application.Dtos;

/// <summary>
/// 系统字典项树查询 DTO
/// </summary>
public sealed class DictItemTreeQueryDto
{
    /// <summary>
    /// 字典主键
    /// </summary>
    public long DictId { get; set; }

    /// <summary>
    /// 是否只返回启用项
    /// </summary>
    public bool OnlyEnabled { get; set; } = true;

    /// <summary>
    /// 最大返回数量
    /// </summary>
    public int Limit { get; set; } = 5000;
}
