import { readonly, ref } from 'vue'

/**
 * 内容区域最大化的组合式函数。
 *
 * 使用模块级单例 ref，所有调用方共享同一份状态，
 * 无需事件总线即可实现跨组件（Header、Tabbar、页面）同步。
 *
 */
const _contentMaximized = ref(false)

export function useContentMaximize() {
  /** 当前是否处于内容最大化模式（只读） */
  const contentIsMaximize = readonly(_contentMaximized)

  /** 进入内容最大化模式（隐藏 header / sidebar / tabbar） */
  function maximize() {
    _contentMaximized.value = true
  }

  /** 退出内容最大化模式 */
  function restore() {
    _contentMaximized.value = false
  }

  /** 切换最大化状态 */
  function toggleMaximize() {
    _contentMaximized.value = !_contentMaximized.value
  }

  return { contentIsMaximize, maximize, restore, toggleMaximize }
}
