// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Runtime.Schema;

/// <summary>
/// 一个没有升级成功的租户独立库
/// </summary>
/// <param name="Database">库名（租户 ID）</param>
/// <param name="Code">错误码</param>
/// <param name="Message">原因</param>
/// <param name="FailedAt">失败的时间</param>
public sealed record SchemaUpgradeFailure(string Database, string Code, string Message, DateTimeOffset FailedAt);
