// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Contracts.Entities;

/// <summary>
/// 记录创建人的实体。值是创建时调用主体的 ID；框架只维护时间字段，操作人字段由 <c>Runtime</c> 在写库时填充。
/// </summary>
public interface IHasCreator
{
    /// <summary>
    /// 创建人（主体 ID）；匿名或系统作业创建时为 <see langword="null"/>。
    /// </summary>
    string? CreatedBy { get; set; }
}
