import type { PiniaPlugin } from 'pinia'

/**
 * Setup Store 的 $reset 插件。
 * 由于 Setup Store 不自带 $reset，此插件在 store 初始化时
 * 深拷贝一份初始快照，重写 $reset 为恢复快照。
 */
export function resetSetupStorePlugin(): PiniaPlugin {
  return ({ store }) => {
    const initialState = JSON.parse(JSON.stringify(store.$state))

    store.$reset = () => {
      store.$patch(($state) => {
        Object.assign($state, JSON.parse(JSON.stringify(initialState)))
      })
    }
  }
}
