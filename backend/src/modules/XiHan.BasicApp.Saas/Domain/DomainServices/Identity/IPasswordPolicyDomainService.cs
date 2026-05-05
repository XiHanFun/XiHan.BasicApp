#region <<版权版本注释>>

// ----------------------------------------------------------------
// Copyright ©2021-Present ZhaiFanhua All Rights Reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.
// FileName:IPasswordPolicyDomainService
// Guid:c3d4e5f6-7890-1234-cdef-234567890abc
// Author:zhaifanhua
// Email:me@zhaifanhua.com
// CreateTime:2026/05/06 00:00:00
// ----------------------------------------------------------------

#endregion <<版权版本注释>>

namespace XiHan.BasicApp.Saas.Domain.DomainServices;

/// <summary>
/// 密码策略领域服务
/// </summary>
/// <remarks>
/// 职责：密码强度校验、历史密码重复检测
/// 不持有任何基础设施依赖，仅包含纯密码策略逻辑
/// </remarks>
public interface IPasswordPolicyDomainService
{
    /// <summary>
    /// 校验密码强度
    /// </summary>
    /// <param name="password">明文密码</param>
    /// <returns>校验结果（通过返回 null，不通过返回错误消息）</returns>
    string? ValidateStrength(string password);

    /// <summary>
    /// 检查密码是否与历史密码重复
    /// </summary>
    /// <param name="passwordHash">新密码哈希</param>
    /// <param name="historicalHashes">历史密码哈希集合</param>
    /// <returns>是否与历史密码重复</returns>
    bool IsDuplicateWithHistory(string passwordHash, IEnumerable<string> historicalHashes);
}
