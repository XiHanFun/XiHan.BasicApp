// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Contracts.Entities;

/// <summary>
/// 记录最后修改人的实体。
/// </summary>
/// <remarks>
/// 值是修改时调用主体的 ID，由 <c>Runtime</c> 在写库时填充。
/// </remarks>
public interface IHasModifier
{
    /// <summary>
    /// 最后修改人（主体 ID）。
    /// </summary>
    /// <remarks>
    /// 未修改过或由系统修改时为 <see langword="null"/>。
    /// </remarks>
    string? ModifiedBy { get; set; }
}
