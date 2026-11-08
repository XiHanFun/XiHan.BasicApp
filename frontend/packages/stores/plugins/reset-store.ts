/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

import type { PiniaPlugin } from 'pinia'

/**
 * 给 Setup Store 补上 $reset，回退到初始化时深拷贝的状态快照
 * store 自定义了 $reset（如要清 localStorage）时优先调用它
 */
export function resetSetupStorePlugin(): PiniaPlugin {
  return ({ store }) => {
    const initialState = JSON.parse(JSON.stringify(store.$state))
    const originalReset = store.$reset

    store.$reset = () => {
      try {
        // 必须绑回 store：options store 的原生 $reset 内部依赖 this.$patch，
        // 裸调用会丢 this 抛 TypeError 并被下方 catch 吞掉，退化成快照回退
        originalReset.call(store)
      }
      catch {
        // 默认 setup store 的 $reset 会抛异常，此时使用初始状态快照回退
        store.$patch(($state) => {
          Object.assign($state, JSON.parse(JSON.stringify(initialState)))
        })
      }
    }
  }
}
