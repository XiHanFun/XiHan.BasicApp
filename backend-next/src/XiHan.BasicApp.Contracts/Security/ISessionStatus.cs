// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

namespace XiHan.BasicApp.Contracts.Security;

/// <summary>
/// 会话状态（Identity 实现）。
/// </summary>
/// <remarks>
/// 与 HTTP 调用的会话门禁同一份判定与缓存：绕过内核管道的入口（实时 Hub）建立连接时核对访问令牌的 <c>sid</c> 仍有效。
/// </remarks>
public interface ISessionStatus
{
    /// <summary>
    /// 判断会话是否仍有效。
    /// </summary>
    /// <remarks>
    /// 有效指未吊销、未过期、用户正常；模仿会话还要求发起人的原会话有效。
    /// </remarks>
    /// <param name="sessionId">会话 ID。</param>
    /// <param name="cancellationToken">取消令牌。</param>
    ValueTask<bool> IsActiveAsync(Guid sessionId, CancellationToken cancellationToken);
}
