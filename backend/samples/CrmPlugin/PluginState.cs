// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using XiHan.Framework.Plugins.Protocol.V1;

namespace CrmPlugin;

/// <summary>
/// 插件的生命周期状态
/// </summary>
/// <remarks>
/// 宿主按 Handshake → Configure → Start 推进，Stop 收尾；次序不对的调用不改状态。只有 Ready 时接受业务调用
/// </remarks>
internal sealed class PluginState
{
    /// <summary>
    /// 串行化状态转移
    /// </summary>
    private readonly Lock _gate = new();

    /// <summary>
    /// 当前状态
    /// </summary>
    private LifecycleState _state = LifecycleState.Declared;

    /// <summary>
    /// 清单原文，握手时原样交给宿主
    /// </summary>
    public string ManifestJson { get; } = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "xihan.manifest.json"));

    /// <summary>
    /// 当前状态
    /// </summary>
    public LifecycleState State
    {
        get
        {
            lock (_gate)
            {
                return _state;
            }
        }
    }

    /// <summary>
    /// 从一个状态推进到下一个
    /// </summary>
    /// <param name="from">要求的当前状态</param>
    /// <param name="to">推进到的状态</param>
    /// <returns>当前状态不是 <paramref name="from"/> 时为假，状态不变</returns>
    public bool TryAdvance(LifecycleState from, LifecycleState to)
    {
        lock (_gate)
        {
            if (_state != from)
            {
                return false;
            }

            _state = to;
            return true;
        }
    }

    /// <summary>
    /// 进入收尾
    /// </summary>
    /// <returns>已经停过时为假</returns>
    public bool TryStop()
    {
        lock (_gate)
        {
            if (_state is LifecycleState.Stopped)
            {
                return false;
            }

            _state = LifecycleState.Stopped;
            return true;
        }
    }
}
