#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ApiLogApplicationMapper
// Guid:486d3b0d-00e3-49e8-b7ad-f16a33783e40
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/01 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

using XiHan.BasicApp.Saas.Application.Dtos;
using XiHan.BasicApp.Saas.Domain.Entities;

namespace XiHan.BasicApp.Saas.Application.Mappers;

/// <summary>
/// API 日志应用层映射器
/// </summary>
public static class ApiLogApplicationMapper
{
    /// <summary>
    /// 映射 API 日志列表项
    /// </summary>
    /// <param name="apiLog">API 日志实体</param>
    /// <returns>API 日志列表项 DTO</returns>
    public static ApiLogListItemDto ToListItemDto(SysApiLog apiLog)
    {
        ArgumentNullException.ThrowIfNull(apiLog);

        return new ApiLogListItemDto
        {
            BasicId = apiLog.BasicId,
            UserId = apiLog.UserId,
            UserName = apiLog.UserName,
            SessionId = apiLog.SessionId,
            RequestId = apiLog.RequestId,
            TraceId = apiLog.TraceId,
            ClientId = apiLog.ClientId,
            AppId = apiLog.AppId,
            ApiPath = apiLog.ApiPath,
            ApiName = apiLog.ApiName,
            Method = apiLog.Method,
            IsSignatureValid = apiLog.IsSignatureValid,
            SignatureType = apiLog.SignatureType,
            StatusCode = apiLog.StatusCode,
            IsSuccess = apiLog.IsSuccess,
            RequestTime = apiLog.RequestTime,
            ResponseTime = apiLog.ResponseTime,
            ExecutionTime = apiLog.ExecutionTime,
            RequestSize = apiLog.RequestSize,
            ResponseSize = apiLog.ResponseSize,
            ApiVersion = apiLog.ApiVersion,
            CreatedTime = apiLog.CreatedTime
        };
    }

    /// <summary>
    /// 映射 API 日志详情
    /// </summary>
    /// <param name="apiLog">API 日志实体</param>
    /// <returns>API 日志详情 DTO</returns>
    public static ApiLogDetailDto ToDetailDto(SysApiLog apiLog)
    {
        ArgumentNullException.ThrowIfNull(apiLog);

        var item = ToListItemDto(apiLog);
        return new ApiLogDetailDto
        {
            BasicId = item.BasicId,
            UserId = item.UserId,
            UserName = item.UserName,
            SessionId = item.SessionId,
            RequestId = item.RequestId,
            TraceId = item.TraceId,
            ClientId = item.ClientId,
            AppId = item.AppId,
            ApiPath = item.ApiPath,
            ApiName = item.ApiName,
            Method = item.Method,
            IsSignatureValid = item.IsSignatureValid,
            SignatureType = item.SignatureType,
            StatusCode = item.StatusCode,
            IsSuccess = item.IsSuccess,
            RequestTime = item.RequestTime,
            ResponseTime = item.ResponseTime,
            ExecutionTime = item.ExecutionTime,
            RequestSize = item.RequestSize,
            ResponseSize = item.ResponseSize,
            ApiVersion = item.ApiVersion,
            CreatedTime = item.CreatedTime,
            CreatedId = apiLog.CreatedId,
            CreatedBy = apiLog.CreatedBy
        };
    }

}
