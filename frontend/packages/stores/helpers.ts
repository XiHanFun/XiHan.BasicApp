import { watch } from 'vue'
import { STORAGE_PREFIX } from '~/constants'
import { LocalStorage } from '~/utils'

const BUILD_TIME_KEY = `${STORAGE_PREFIX}build_time`

/**
 * BUILD_TIME 缓存失效：
 * 当 `__APP_BUILD_TIME__` 变化时清除所有带前缀的偏好缓存，
 * 强制使用新默认值，解决"用户本地偏好锁死新功能"的问题。
 */
export function invalidateCacheIfBuildTimeChanged() {
  const currentBuildTime = typeof __APP_BUILD_TIME__ === 'string' ? __APP_BUILD_TIME__ : ''
  if (!currentBuildTime)
    return

  const savedBuildTime = LocalStorage.get<string>(BUILD_TIME_KEY)
  if (savedBuildTime === currentBuildTime)
    return

  const keysToRemove: string[] = []
  for (let i = 0; i < localStorage.length; i++) {
    const key = localStorage.key(i)
    if (key?.startsWith(STORAGE_PREFIX) && key !== BUILD_TIME_KEY) {
      keysToRemove.push(key)
    }
  }
  keysToRemove.forEach(key => localStorage.removeItem(key))
  LocalStorage.set(BUILD_TIME_KEY, currentBuildTime)
}

/** 监听 ref 变化并自动写入 localStorage */
export function bindPersist<T>(key: string, source: { value: T }) {
  watch(source, (value) => {
    LocalStorage.set(key, value)
  })
}

/** 立即更新 ref 并写入 localStorage */
export function save<T>(key: string, target: { value: T }, value: T) {
  target.value = value
  LocalStorage.set(key, value)
}
