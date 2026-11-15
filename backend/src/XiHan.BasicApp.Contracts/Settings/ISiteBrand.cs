// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Contracts.Settings;

/// <summary>
/// 站点的系统名（Settings 实现）
/// </summary>
/// <remarks>
/// 取站点设置 <c>settings.site</c> 的系统名：租户取它的品牌，没设沿用平台的。Notification 给邮件、短信与机器人消息的模板变量 <c>brand</c> 补值时用
/// 没有实现时用应用名
/// </remarks>
public interface ISiteBrand
{
    /// <summary>
    /// 一侧生效的系统名
    /// </summary>
    /// <param name="owner">所属侧：租户 ID 或平台</param>
    /// <param name="cancellationToken">取消令牌</param>
    /// <returns>系统名；站点设置都没写即应用名，参数写坏了（手工改库）即失败</returns>
    Task<Result<string>> NameOfAsync(string owner, CancellationToken cancellationToken);
}
