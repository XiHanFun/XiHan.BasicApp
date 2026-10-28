// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Auditing.Domain.Entities;

/// <summary>
/// 访问结果：由调用结果的错误类别归并。
/// </summary>
public enum AccessResult
{
    /// <summary>
    /// 成功。
    /// </summary>
    Success = 0,

    /// <summary>
    /// 业务失败（校验、冲突、超时、取消等）。
    /// </summary>
    Failed = 1,

    /// <summary>
    /// 越权。
    /// </summary>
    Forbidden = 2,

    /// <summary>
    /// 未认证或凭据无效。
    /// </summary>
    Unauthorized = 3,

    /// <summary>
    /// 资源不存在。
    /// </summary>
    NotFound = 4,

    /// <summary>
    /// 服务端故障（未处理异常、依赖不可用）。
    /// </summary>
    ServerError = 5,
}
