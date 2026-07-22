// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.ComponentModel;

namespace XiHan.BasicApp.Saas.Domain.Entities;

/// <summary>
/// 操作执行结果枚举（记录一次业务操作的最终结果）
/// 注意：勿与启用状态 EnableStatus 混淆，本枚举表达"成功/失败/部分成功"
/// </summary>
public enum OperationExecuteResult
{
    /// <summary>
    /// 成功
    /// </summary>
    [Description("成功")]
    Success = 0,

    /// <summary>
    /// 失败
    /// </summary>
    [Description("失败")]
    Failed = 1,

    /// <summary>
    /// 部分成功（批量操作中部分项成功、部分项失败）
    /// </summary>
    [Description("部分成功")]
    PartialSuccess = 2
}
