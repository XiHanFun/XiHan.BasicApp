// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Runtime.Schema;

namespace XiHan.BasicApp.Host;

/// <summary>
/// 启动前的结构升级。
/// </summary>
/// <remarks>
/// 在开始监听之前执行（方案 5.14）：升级完成前这个节点不接请求，失败即中断启动；关掉 <c>BasicApp:Schema:AutoUpgrade</c> 时跳过。
/// 别的节点升级共享的库期间，已在运行的节点按台账返回 503（<see cref="Runtime.Middleware.MaintenanceModeMiddleware"/>）。
/// </remarks>
public static partial class SchemaStartup
{
    /// <summary>
    /// 按配置执行结构升级。
    /// </summary>
    /// <returns>可以接着启动时为 <see langword="true"/>。</returns>
    public static async Task<bool> UpgradeAsync(WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);
        if (!app.Services.GetRequiredService<SchemaOptions>().AutoUpgrade)
        {
            return true;
        }

        var upgraded = await app.Services.GetRequiredService<SchemaUpgrader>().UpgradeAsync(CancellationToken.None).ConfigureAwait(false);
        if (upgraded.IsFailure)
        {
            LogFailed(app.Logger, upgraded.Error!.Code, upgraded.Error.Message);
            return false;
        }

        return true;
    }

    [LoggerMessage(Level = LogLevel.Critical, Message = "Startup aborted: schema upgrade failed ({Code}): {Message}")]
    private static partial void LogFailed(ILogger logger, string code, string message);
}
