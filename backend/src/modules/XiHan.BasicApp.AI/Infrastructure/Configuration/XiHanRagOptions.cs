#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:XiHanRagOptions
// Guid:a11c0de0-5008-4a10-9a00-00000000ai57
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/07/05 16:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.AI.Infrastructure.Configuration;

/// <summary>
/// RAG 基础设施配置（向量库 Qdrant 连接等，属部署级基础设施配置，走 appsettings）
/// </summary>
public sealed class XiHanRagOptions
{
    /// <summary>
    /// 配置节名
    /// </summary>
    public const string SectionName = "XiHan:AI:Rag";

    /// <summary>
    /// Qdrant 主机（gRPC）
    /// </summary>
    public string QdrantHost { get; set; } = "localhost";

    /// <summary>
    /// Qdrant gRPC 端口
    /// </summary>
    public int QdrantPort { get; set; } = 6334;

    /// <summary>
    /// 是否 HTTPS（Qdrant Cloud 用 true + ApiKey）
    /// </summary>
    public bool QdrantHttps { get; set; } = false;

    /// <summary>
    /// Qdrant API Key（云端鉴权用）
    /// </summary>
    public string? QdrantApiKey { get; set; }

    /// <summary>
    /// 检索默认返回条数
    /// </summary>
    public int DefaultTopK { get; set; } = 5;
}
