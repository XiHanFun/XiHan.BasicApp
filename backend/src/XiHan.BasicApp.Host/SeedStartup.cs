// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.BasicApp.Runtime.Seeding;

namespace XiHan.BasicApp.Host;

/// <summary>
/// 启动时的种子
/// </summary>
/// <remarks>
/// 打开 <c>BasicApp:Seed:RunOnStartup</c> 时，在应用启动之后（种子要求应用处于服务状态）执行一遍全部种子，失败即停止应用；缺省关，由 <c>seed</c> 命令执行
/// </remarks>
public static partial class SeedStartup
{
    /// <summary>
    /// 按配置执行种子
    /// </summary>
    /// <returns>可以接着服务时为 <see langword="true"/></returns>
    public static async Task<bool> RunAsync(WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);
        if (!app.Services.GetRequiredService<SeedOptions>().RunOnStartup)
        {
            return true;
        }

        var seeded = await app.Services.GetRequiredService<SeedRunner>().RunAsync(null, app.Lifetime.ApplicationStopping).ConfigureAwait(false);
        if (seeded.IsFailure)
        {
            LogFailed(app.Logger, seeded.Error!.Code, seeded.Error.Message);
            return false;
        }

        return true;
    }

    [LoggerMessage(Level = LogLevel.Critical, Message = "Startup aborted: seeding failed ({Code}): {Message}")]
    private static partial void LogFailed(ILogger logger, string code, string message);
}
