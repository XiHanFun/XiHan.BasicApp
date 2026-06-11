#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:SaasLogMappingHelper
// Guid:7f2d4c9e-a3f4-4bdb-9d42-6b6b9a1d4e30
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/03/08 20:05:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using Microsoft.AspNetCore.Http;
using XiHan.BasicApp.Saas.Domain.Entities;
using XiHan.BasicApp.Saas.Domain.Enums;

namespace XiHan.BasicApp.Saas.Infrastructure.Logging;

/// <summary>
/// SaaS 日志映射辅助方法
/// </summary>
internal static class SaasLogMappingHelper
{
    /// <summary>
    /// 字符串裁剪并转空
    /// </summary>
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
    public static string TrimOrDefault(string? value, int maxLength, string defaultValue)
    {
        return TrimOrNull(value, maxLength) ?? defaultValue;
    }

    /// <summary>
    /// 规范化耗时毫秒
    /// </summary>
    public static long NormalizeElapsed(long elapsedMilliseconds)
    {
        return elapsedMilliseconds < 0 ? 0 : elapsedMilliseconds;
    }

    /// <summary>
    /// 根据状态码映射访问结果
    /// </summary>
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
            "REVIEW" => OperationType.Review,
            "APPROVE" => OperationType.Approve,
            "STARTTASK" => OperationType.StartTask,
            "EXECUTE" => OperationType.Execute,
            "RESTORE" => OperationType.Restore,
            _ => OperationType.Other
        };
    }

    /// <summary>
    /// 根据 Action 名称语义映射操作类型，无法识别时回退到 HTTP 方法映射
    /// </summary>
    /// <remarks>
    /// 操作日志面向业务行为（新增/修改/删除/审核/导入/导出/审批/发起任务/执行命令），
    /// 优先从动作命名识别语义，避免 POST 一律记为"新增"。
    /// </remarks>
    public static OperationType ResolveOperationTypeByAction(string? actionName, string? httpMethod)
    {
        if (!string.IsNullOrWhiteSpace(actionName))
        {
            var name = actionName.Trim();
            if (ContainsAny(name, "Import"))
            {
                return OperationType.Import;
            }

            if (ContainsAny(name, "Export", "Download"))
            {
                return OperationType.Export;
            }

            if (ContainsAny(name, "Review", "Audit"))
            {
                return OperationType.Review;
            }

            if (ContainsAny(name, "Approve", "Approval", "Reject"))
            {
                return OperationType.Approve;
            }

            if (ContainsAny(name, "Trigger", "Dispatch")
                || (ContainsAny(name, "Start", "Launch") && ContainsAny(name, "Task", "Job", "Workflow", "Process")))
            {
                return OperationType.StartTask;
            }

            if (ContainsAny(name, "Execute", "Run", "Command", "Invoke"))
            {
                return OperationType.Execute;
            }

            if (ContainsAny(name, "Restore", "Recover"))
            {
                return OperationType.Restore;
            }

            if (ContainsAny(name, "Create", "Add", "Insert", "Register", "Upload"))
            {
                return OperationType.Create;
            }

            if (ContainsAny(name, "Delete", "Remove", "Clear", "Revoke"))
            {
                return OperationType.Delete;
            }

            if (ContainsAny(name, "Update", "Modify", "Edit", "Change", "Set", "Enable", "Disable", "Save", "Reset", "Sort", "Move", "Assign", "Grant", "Bind", "Unbind", "Switch"))
            {
                return OperationType.Update;
            }

            if (ContainsAny(name, "Get", "Page", "List", "Query", "Search", "Find", "Detail", "Batch"))
            {
                return OperationType.Query;
            }
        }

        return ResolveOperationTypeByHttpMethod(httpMethod);
    }

    /// <summary>
    /// 名称包含任一关键字（忽略大小写）
    /// </summary>
    private static bool ContainsAny(string value, params string[] keywords)
    {
        return keywords.Any(keyword => value.Contains(keyword, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 根据响应结果映射操作执行结果
    /// </summary>
    public static OperationExecuteResult ResolveResult(int statusCode, string? errorMessage)
    {
        return statusCode < 400 && string.IsNullOrWhiteSpace(errorMessage)
            ? OperationExecuteResult.Success
            : OperationExecuteResult.Failed;
    }

    /// <summary>
    /// 根据设备信息映射设备类型
    /// </summary>
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
    public static int ResolveRiskLevel(OperationType operationType)
    {
        return operationType switch
        {
            OperationType.Delete => 4,
            OperationType.Export => 4,
            OperationType.Execute => 4,
            OperationType.Update => 3,
            OperationType.Restore => 3,
            OperationType.Review => 3,
            OperationType.Approve => 3,
            OperationType.Import => 3,
            OperationType.Create => 2,
            OperationType.StartTask => 2,
            _ => 1
        };
    }
}
