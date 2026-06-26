import { defineStore } from 'pinia'
import { ref } from 'vue'
import { SPLIT_VIEW_KEY } from '~/constants'
import { SessionStorage } from '~/utils'
import { SetupStoreId } from './store-ids'

/** 持久化载荷（SessionStorage，与标签页持久化同生命周期：刷新保留、关浏览器清除） */
interface PersistedSplitView {
  active: boolean
  left: string
  right: string
  ratio: number
  reversed: boolean
}

/**
 * 分屏对照（Split View）：把「当前标签」与「另一个已打开标签」合并成一个分屏标签，
 * 内容区左右并排——锚定标签（主视图 = 真实路由）+ 副标签（应用内直接渲染该路由组件，
 * 无 iframe、零重启零闪烁），类似 HarmonyOS 多窗口 / iPad Split View。
 *
 * 模型：active + leftPath（锚定）+ rightPath（副，从标签栏隐藏）+ reversed（视觉反转）。
 * 「左右互换」= 翻转 reversed（纯 CSS order 交换，两侧组件实例原地保留，不重挂载不刷新）。
 * 仅当当前路由 === leftPath 时显示分屏；状态持久化到 SessionStorage，刷新页面不消失。
 */
export const useSplitViewStore = defineStore(SetupStoreId.SplitView, () => {
  const saved = SessionStorage.get<PersistedSplitView>(SPLIT_VIEW_KEY)

  const active = ref(saved?.active ?? false)
  /** 锚定标签（主视图 = 真实路由；其标签菜单显示「关闭分屏」） */
  const leftPath = ref(saved?.left ?? '')
  /** 副标签（合并后从标签栏隐藏，由 SplitPane 直接渲染组件） */
  const rightPath = ref(saved?.right ?? '')
  /** 视觉左侧 pane 的宽度占比 [0.2, 0.8] */
  const ratio = ref(saved?.ratio ?? 0.5)
  /** 视觉反转：false=锚定在左；true=锚定在右（互换的结果，纯视觉、不重挂载） */
  const reversed = ref(saved?.reversed ?? false)

  function persist(): void {
    SessionStorage.set(SPLIT_VIEW_KEY, {
      active: active.value,
      left: leftPath.value,
      right: rightPath.value,
      ratio: ratio.value,
      reversed: reversed.value,
    } satisfies PersistedSplitView)
  }

  /** 开启分屏：left 锚定标签 + right 副标签（锚定默认在左） */
  function open(left: string, right: string): void {
    if (!left || !right || left === right) {
      return
    }
    leftPath.value = left
    rightPath.value = right
    reversed.value = false
    active.value = true
    persist()
  }

  /** 切换副标签（不能与锚定相同） */
  function setRightPath(path: string): void {
    if (path && path !== leftPath.value) {
      rightPath.value = path
      persist()
    }
  }

  /** 切换锚定标签（不能与副标签相同；导航由调用方负责，布局层在路由抵达时调用对齐） */
  function setLeftPath(path: string): void {
    if (path && path !== rightPath.value) {
      leftPath.value = path
      persist()
    }
  }

  /** 设置视觉左侧宽度占比（夹在 [0.2, 0.8]） */
  function setRatio(value: number): void {
    ratio.value = Math.min(0.8, Math.max(0.2, value))
    persist()
  }

  /**
   * 左右互换：翻转视觉顺序（CSS order），两侧组件实例原地保留——不导航、不重挂载、不刷新。
   * ratio（视觉左侧占比）保持不变 → 分割线位置固定，互换前后左侧始终一样宽。
   */
  function toggleReversed(): void {
    if (!active.value) {
      return
    }
    reversed.value = !reversed.value
    persist()
  }

  /** 锚定/副标签互换（用户经菜单直接导航到副标签时对齐用；重置视觉反转） */
  function swapPaths(): void {
    if (!active.value) {
      return
    }
    const left = leftPath.value
    leftPath.value = rightPath.value
    rightPath.value = left
    reversed.value = false
    persist()
  }

  /** 关闭分屏 */
  function close(): void {
    active.value = false
    leftPath.value = ''
    rightPath.value = ''
    reversed.value = false
    persist()
  }

  /** 是否为分屏锚定标签（其菜单显示「关闭分屏」） */
  function isSplitTab(path: string): boolean {
    return active.value && path === leftPath.value
  }

  /** 是否为被合并的副标签（应从标签栏隐藏） */
  function isMergedTab(path: string): boolean {
    return active.value && path === rightPath.value
  }

  return {
    active,
    leftPath,
    rightPath,
    ratio,
    reversed,
    open,
    setRightPath,
    setLeftPath,
    setRatio,
    toggleReversed,
    swapPaths,
    close,
    isSplitTab,
    isMergedTab,
  }
})
