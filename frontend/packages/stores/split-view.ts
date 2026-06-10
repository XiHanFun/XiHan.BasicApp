import { defineStore } from 'pinia'
import { ref } from 'vue'
import { SetupStoreId } from './store-ids'

/**
 * 分屏对照（Split View）：把「当前标签」与「另一个已打开标签」合并成一个分屏标签，
 * 内容区左右并排——左为锚定标签（主视图），右为另一标签（iframe 内容-only 模式，
 * 拥有独立路由上下文，类似 HarmonyOS 多窗口 / iPad Split View）。
 *
 * 模型：active + leftPath（分屏锚定标签）+ rightPath（被合并到右侧、从标签栏隐藏的标签）。
 * 仅当当前路由 === leftPath 时显示分屏；右标签从标签栏隐藏（视为已并入左标签）。
 * 纯视图态，不持久化（刷新后关闭）。
 */
export const useSplitViewStore = defineStore(SetupStoreId.SplitView, () => {
  const active = ref(false)
  /** 分屏锚定标签（左侧 = 主视图，菜单显示「关闭分屏」） */
  const leftPath = ref('')
  /** 右侧 pane 标签（合并后从标签栏隐藏） */
  const rightPath = ref('')
  /** 左侧宽度占比 [0.2, 0.8] */
  const ratio = ref(0.5)

  /** 开启分屏：left 锚定标签 + right 右侧标签 */
  function open(left: string, right: string): void {
    if (!left || !right || left === right) {
      return
    }
    leftPath.value = left
    rightPath.value = right
    active.value = true
  }

  /** 切换右侧标签（不能与左侧相同） */
  function setRightPath(path: string): void {
    if (path && path !== leftPath.value) {
      rightPath.value = path
    }
  }

  /** 设置左侧宽度占比（夹在 [0.2, 0.8]） */
  function setRatio(value: number): void {
    ratio.value = Math.min(0.8, Math.max(0.2, value))
  }

  /** 左右互换（仅交换路径；导航由调用方负责，布局层在路由抵达右路径时调用本方法对齐） */
  function swapPaths(): void {
    if (!active.value) {
      return
    }
    const left = leftPath.value
    leftPath.value = rightPath.value
    rightPath.value = left
  }

  /** 关闭分屏 */
  function close(): void {
    active.value = false
    leftPath.value = ''
    rightPath.value = ''
  }

  /** 是否为分屏锚定标签（其菜单显示「关闭分屏」） */
  function isSplitTab(path: string): boolean {
    return active.value && path === leftPath.value
  }

  /** 是否为被合并到右侧、应从标签栏隐藏的标签 */
  function isMergedTab(path: string): boolean {
    return active.value && path === rightPath.value
  }

  return {
    active,
    leftPath,
    rightPath,
    ratio,
    open,
    setRightPath,
    setRatio,
    swapPaths,
    close,
    isSplitTab,
    isMergedTab,
  }
})
