#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:RbacLogMappingHelper
// Guid:7f2d4c9e-a3f4-4bdb-9d42-6b6b9a1d4e30
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/08 20:05:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Http;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Infrastructure.Logging;

/// <summary>
/// RBAC 日志映射辅助方法
/// </summary>
internal static class RbacLogMappingHelper
{
    /// <summary>
    /// 字符串裁剪并转空
    /// </summary>
    /// <param name="value"></param>
    /// <param name="maxLength"></param>
    /// <returns></returns>
    public static string? TrimOrNull(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
    }

    /// <summary>
    /// 字符串裁剪并提供默认值
    /// </summary>
    /// <param name="value"></param>
    /// <param name="maxLength"></param>
    /// <param name="defaultValue"></param>
    /// <returns></returns>
    public static string TrimOrDefault(string? value, int maxLength, string defaultValue)
    {
        return TrimOrNull(value, maxLength) ?? defaultValue;
    }

    /// <summary>
    /// 规范化耗时毫秒
    /// </summary>
    /// <param name="elapsedMilliseconds"></param>
    /// <returns></returns>
    public static long NormalizeElapsed(long elapsedMilliseconds)
    {
        return elapsedMilliseconds < 0 ? 0 : elapsedMilliseconds;
    }

    /// <summary>
    /// 根据状态码映射访问结果
    /// </summary>
    /// <param name="statusCode"></param>
    /// <returns></returns>
    public static AccessResult ResolveAccessResult(int statusCode)
    {
        return statusCode switch
        {
            >= 500 => AccessResult.ServerError,
            StatusCodes.Status401Unauthorized => AccessResult.Unauthorized,
            StatusCodes.Status403Forbidden => AccessResult.Forbidden,
            StatusCodes.Status404NotFound => AccessResult.NotFound,
            >= 400 => AccessResult.Failed,
            _ => AccessResult.Success
        };
    }

    /// <summary>
    /// 根据 HTTP 方法映射操作类型
    /// </summary>
    /// <param name="httpMethod"></param>
    /// <returns></returns>
    public static OperationType ResolveOperationTypeByHttpMethod(string? httpMethod)
    {
        if (string.IsNullOrWhiteSpace(httpMethod))
        {
            return OperationType.Other;
        }

        return httpMethod.Trim().ToUpperInvariant() switch
        {
            "GET" or "HEAD" or "OPTIONS" => OperationType.Query,
            "POST" => OperationType.Create,
            "PUT" or "PATCH" => OperationType.Update,
            "DELETE" => OperationType.Delete,
            _ => OperationType.Other
        };
    }

    /// <summary>
    /// 根据操作名称映射操作类型
    /// </summary>
    /// <param name="operationType"></param>
    /// <returns></returns>
    public static OperationType ResolveOperationTypeByName(string? operationType)
    {
        if (string.IsNullOrWhiteSpace(operationType))
        {
            return OperationType.Other;
        }

        return operationType.Trim().ToUpperInvariant() switch
        {
            "LOGIN" => OperationType.Login,
            "LOGOUT" => OperationType.Logout,
            "QUERY" => OperationType.Query,
            "CREATE" => OperationType.Create,
            "UPDATE" => OperationType.Update,
            "DELETE" => OperationType.Delete,
            "IMPORT" => OperationType.Import,
            "EXPORT" => OperationType.Export,
            _ => OperationType.Other
        };
    }

    /// <summary>
    /// 根据响应结果映射是否成功
    /// </summary>
    /// <param name="statusCode"></param>
    /// <param name="errorMessage"></param>
    /// <returns></returns>
    public static YesOrNo ResolveStatus(int statusCode, string? errorMessage)
    {
        return statusCode < 400 && string.IsNullOrWhiteSpace(errorMessage)
            ? YesOrNo.Yes
            : YesOrNo.No;
    }

    /// <summary>
    /// 根据设备信息映射设备类型
    /// </summary>
    /// <param name="deviceName"></param>
    /// <returns></returns>
    public static DeviceType ResolveDeviceType(string? deviceName)
    {
        if (string.IsNullOrWhiteSpace(deviceName))
        {
            return DeviceType.Unknown;
        }

        var normalized = deviceName.Trim().ToLowerInvariant();
        if (normalized.Contains("iphone", StringComparison.Ordinal) ||
            normalized.Contains("ipad", StringComparison.Ordinal) ||
            normalized.Contains("ios", StringComparison.Ordinal))
        {
            return DeviceType.iOS;
        }

        if (normalized.Contains("android", StringComparison.Ordinal))
        {
            return DeviceType.Android;
        }

        if (normalized.Contains("windows", StringComparison.Ordinal))
        {
            return DeviceType.Windows;
        }

        if (normalized.Contains("mac", StringComparison.Ordinal))
        {
            return DeviceType.macOS;
        }

        if (normalized.Contains("linux", StringComparison.Ordinal))
        {
            return DeviceType.Linux;
        }

        if (normalized.Contains("tablet", StringComparison.Ordinal))
        {
            return DeviceType.Tablet;
        }

        if (normalized.Contains("mini", StringComparison.Ordinal))
        {
            return DeviceType.MiniProgram;
        }

        if (normalized.Contains("api", StringComparison.Ordinal))
        {
            return DeviceType.Api;
        }

        return DeviceType.Web;
    }

    /// <summary>
    /// 根据操作类型映射风险等级
    /// </summary>
    /// <param name="operationType"></param>
    /// <returns></returns>
    public static int ResolveRiskLevel(OperationType operationType)
    {
        return operationType switch
        {
            OperationType.Delete => 4,
            OperationType.Update => 3,
            OperationType.Create => 2,
            _ => 1
        };
    }
}
