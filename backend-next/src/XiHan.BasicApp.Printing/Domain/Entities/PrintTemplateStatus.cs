// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Printing.Domain.Entities;

/// <summary>
/// 打印模板的启停状态。
/// </summary>
/// <remarks>
/// 停用的模板解析不到，只有停用的模板能删除。
/// </remarks>
public enum PrintTemplateStatus
{
    /// <summary>
    /// 启用。
    /// </summary>
    Active = 0,

    /// <summary>
    /// 停用。
    /// </summary>
    Disabled = 1,
}
