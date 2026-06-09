import { defineStore } from 'pinia'
import { ref } from 'vue'
import { SetupStoreId } from './store-ids'

/**
 * 分屏对照（Split View）：内容区左右并排，左为当前页（主路由），右为另一标签页。
 *
 * 右侧 pane 用 iframe 加载目标路径的「内容-only 模式」(?__pane=1)，从而拥有独立的
 * 路由上下文（各自的 useRoute / 筛选互不串扰），类似 HarmonyOS 多窗口 / iPad Split View。
 * 纯视图态，不持久化（刷新后关闭）。
 */
export const useSplitViewStore = defineStore(SetupStoreId.SplitView, () => {
  /** 是否开启分屏 */
  const active = ref(false)
  /** 右侧 pane 当前展示的路径（取自已打开标签） */
  const panePath = ref('')
  /** 左侧宽度占比 [0.2, 0.8] */
  const ratio = ref(0.5)

  /** 开启分屏并在右侧打开指定路径 */
  function open(path: string): void {
    if (!path) {
      return
    }
    panePath.value = path
    active.value = true
  }

  /** 切换右侧 pane 路径 */
  function setPanePath(path: string): void {
    if (path) {
      panePath.value = path
    }
  }

  /** 设置左侧宽度占比（夹在 [0.2, 0.8]） */
  function setRatio(value: number): void {
    ratio.value = Math.min(0.8, Math.max(0.2, value))
  }

  /** 关闭分屏 */
  function close(): void {
    active.value = false
  }

  return { active, panePath, ratio, open, setPanePath, setRatio, close }
})
