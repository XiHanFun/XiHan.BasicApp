// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Auditing.Domain.Entities;

/// <summary>
/// 数据变更的种类。
/// </summary>
public enum DiffKind
{
    /// <summary>
    /// 新增。
    /// </summary>
    Insert = 0,

    /// <summary>
    /// 修改（软删除也是修改）。
    /// </summary>
    Update = 1,

    /// <summary>
    /// 物理删除。
    /// </summary>
    Delete = 2,
}
