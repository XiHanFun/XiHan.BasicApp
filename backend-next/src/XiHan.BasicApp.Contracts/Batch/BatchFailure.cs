// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Contracts.Batch;

/// <summary>
/// 批量操作里失败的一项。
/// </summary>
/// <param name="Id">目标对象 ID。</param>
/// <param name="Code">失败的错误码（<c>&lt;包&gt;_&lt;KIND&gt;_&lt;NNN&gt;</c>）。</param>
public sealed record BatchFailure(Guid Id, string Code);
