#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:ICodeGenDataSourceDomainService
// Guid:c0de9e00-0a01-4a00-9000-000000000a01
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/06/20 10:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.CodeGeneration.Domain.DomainServices;

/// <summary>
/// 代码生成数据源领域服务
/// </summary>
/// <remarks>
/// 在 <c>AddCodeGenerationDomainServices</c> 中显式注册为 Scoped（实现依赖 Scoped 仓储），与其余领域服务保持一致。
/// </remarks>
public interface ICodeGenDataSourceDomainService
{
    /// <summary>
    /// 创建数据源
    /// </summary>
    Task<CodeGenDataSourceCommandResult> CreateDataSourceAsync(CodeGenDataSourceCreateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新数据源
    /// </summary>
    Task<CodeGenDataSourceCommandResult> UpdateDataSourceAsync(CodeGenDataSourceUpdateCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新数据源状态
    /// </summary>
    Task<CodeGenDataSourceCommandResult> UpdateDataSourceStatusAsync(CodeGenDataSourceStatusChangeCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除数据源（软删）
    /// </summary>
    Task DeleteDataSourceAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 测试数据源连接（只读探测），并回写最后测试结果
    /// </summary>
    Task<CodeGenDataSourceConnectionTestResult> TestConnectionAsync(long id, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取数据源的运行期连接信息（已解密）
    /// </summary>
    /// <remarks>
    /// 供动态连接注册使用，是"连接串构建 + 解密"的唯一出口，避免第二处实现。
    /// 数据源不存在或已停用时抛异常（fail-closed）：宁可明确报错，也不要静默回落到主库
    /// 让用户以为自己导的是外部库、实际导的是本系统的表。
    /// </remarks>
    Task<CodeGenDataSourceConnectionInfo> GetConnectionInfoAsync(long dataSourceId, CancellationToken cancellationToken = default);
}
