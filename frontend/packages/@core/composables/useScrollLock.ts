import { readonly, ref } from 'vue'

/**
 * 锁定/解锁 body 滚动（常用于模态框、抽屉等场景）。
 * 通过保存并恢复 overflow 样式，避免出现滚动条跳动。
 */
export function useScrollLock() {
  const isLocked = ref(false)
  let savedOverflow = ''

  function lock() {
    if (isLocked.value) return
    savedOverflow = document.body.style.overflow
    document.body.style.overflow = 'hidden'
    isLocked.value = true
  }

  function unlock() {
    if (!isLocked.value) return
    document.body.style.overflow = savedOverflow
    isLocked.value = false
  }

  return { isLocked: readonly(isLocked), lock, unlock }
}
