// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Globalization;
using XiHan.BasicApp.Saas.Domain.Repositories;

namespace XiHan.BasicApp.Workflow.Application.Business;

/// <summary>
/// 审批相关用户的显示名解析（受理人、办理人、发起人）
/// </summary>
/// <remarks>
/// 工作流里的人一律以用户主键字符串记录；显示名取真实姓名 → 昵称 → 用户名。
/// 账号按主键跨注册地读取（租户的外部成员注册在别处），查不到的主键原样显示。
/// </remarks>
public sealed class WorkflowUserNameResolver
{
    private readonly IUserRepository _userRepository;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="userRepository">用户仓储</param>
    public WorkflowUserNameResolver(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    /// <summary>
    /// 批量解析显示名
    /// </summary>
    /// <param name="userIds">用户标识（主键字符串；非数值标识原样返回）</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>用户标识 → 显示名</returns>
    public async Task<IReadOnlyDictionary<string, string>> ResolveAsync(IEnumerable<string?> userIds, CancellationToken cancellationToken = default)
    {
        var texts = userIds
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Select(id => id!.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        var result = texts.ToDictionary(id => id, id => id, StringComparer.Ordinal);
        var ids = texts
            .Select(id => long.TryParse(id, NumberStyles.None, CultureInfo.InvariantCulture, out var value) ? value : 0)
            .Where(id => id > 0)
            .Distinct()
            .ToList();
        if (ids.Count == 0)
        {
            return result;
        }

        var users = await _userRepository.GetListByIdsIgnoreTenantAsync(ids, cancellationToken);
        foreach (var user in users)
        {
            var name = !string.IsNullOrWhiteSpace(user.RealName) ? user.RealName
                : !string.IsNullOrWhiteSpace(user.NickName) ? user.NickName
                : user.UserName;
            result[user.BasicId.ToString(CultureInfo.InvariantCulture)] = name;
        }

        return result;
    }

    /// <summary>
    /// 解析单个用户显示名
    /// </summary>
    /// <param name="userId">用户标识</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>显示名（标识为空返回 null）</returns>
    public async Task<string?> ResolveOneAsync(string? userId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return null;
        }

        var names = await ResolveAsync([userId], cancellationToken);
        return names.GetValueOrDefault(userId.Trim());
    }
}
