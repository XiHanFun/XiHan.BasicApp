// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.AI.Abstractions.Rag;

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

    /// <summary>
    /// 向量集合名
    /// </summary>
    public string CollectionName { get; set; } = KnowledgeVectorOptions.DefaultCollectionName;

    /// <summary>
    /// 嵌入向量维度（须与 provider 所配嵌入模型的输出维度一致）
    /// </summary>
    /// <remarks>常见取值：text-embedding-3-small=1536、bge-m3=1024、gte-Qwen2-1.5B-instruct=1536。</remarks>
    public int EmbeddingDimensions { get; set; } = KnowledgeVectorOptions.DefaultDimensions;
}
