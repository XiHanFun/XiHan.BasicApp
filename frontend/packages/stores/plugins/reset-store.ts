import type { PiniaPlugin } from 'pinia'

/**
 * Setup Store 的 $reset 插件。
 * 由于 Setup Store 不自带 $reset，此插件在 store 初始化时
 * 深拷贝一份初始快照，为未自定义 $reset 的 store 提供快照回退。
 * 若 store 已自定义 $reset（如需清理 localStorage），则优先调用自定义实现。
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
