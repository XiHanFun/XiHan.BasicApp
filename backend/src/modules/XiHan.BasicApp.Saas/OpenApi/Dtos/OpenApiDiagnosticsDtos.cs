#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:OpenApiDiagnosticsDtos
// Guid:5a91c3e7-06d4-4b28-9f51-3c7e8a2b6d40
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/18 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.OpenApi.Dtos;

/// <summary>
/// 开放接口连通性自测结果 DTO
/// </summary>
public sealed class OpenApiPingDto
{
    /// <summary>
    /// 是否通过
    /// </summary>
    public bool Ok { get; set; } = true;

    /// <summary>
    /// 结果说明
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 经签名校验解析出的调用方 AccessKey
    /// </summary>
    public string? AccessKey { get; set; }

    /// <summary>
    /// 凭证归属用户标识（数据库凭证有值）
    /// </summary>
    public long? OwnerUserId { get; set; }

    /// <summary>
    /// 服务器时间（UTC，供调用方校准签名时间戳）
    /// </summary>
    public DateTimeOffset ServerTimeUtc { get; set; }
}

/// <summary>
/// 开放接口回显自测入参 DTO
/// </summary>
public sealed class OpenApiEchoInputDto
{
    /// <summary>
    /// 任意文本（原样回显，用于验证请求体完整性与内容签名）
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// 任意数值（原样回显）
    /// </summary>
    public int? Number { get; set; }
}

/// <summary>
/// 开放接口回显自测结果 DTO
/// </summary>
public sealed class OpenApiEchoDto
{
    /// <summary>
    /// 是否通过
    /// </summary>
    public bool Ok { get; set; } = true;

    /// <summary>
    /// 结果说明
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// 经签名校验解析出的调用方 AccessKey
    /// </summary>
    public string? AccessKey { get; set; }

    /// <summary>
    /// 原样回显的入参（能完整回显即说明请求体未被篡改且内容签名一致）
    /// </summary>
    public OpenApiEchoInputDto? Echo { get; set; }

    /// <summary>
    /// 服务器时间（UTC）
    /// </summary>
    public DateTimeOffset ServerTimeUtc { get; set; }
}
