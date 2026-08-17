import type { Ref } from 'vue'
import { onBeforeUnmount, ref } from 'vue'

/**
 * 行悬停速览（Peek & Pop）的悬停意图管理。
 *
 * - 悬停某行约 delay 毫秒后显示速览（防误触）；
 * - 已显示时移到相邻行即时切换内容（不重新等待）；
 * - 离行短暂宽限（行间缝隙不闪烁），滚动 / 按下 / 键盘即时隐藏。
 */
export interface RowPeekState<TRow> {
  visible: Ref<boolean>
  row: Ref<TRow | null>
  x: Ref<number>
  y: Ref<number>
  /** 绑定到 NDataTable :row-props 的工厂 */
  rowProps: (row: TRow) => Record<string, unknown>
  hide: () => void
}

export function useRowPeek<TRow extends object>(options?: {
  /** 悬停出现延迟（默认 450ms） */
  delay?: number
  /** 启用判定（如移动端禁用） */
  enabled?: () => boolean
}): RowPeekState<TRow> {
  const delay = options?.delay ?? 450
  const visible = ref(false)
  const row = ref(null) as Ref<TRow | null>
  const x = ref(0)
  const y = ref(0)

  let showTimer: ReturnType<typeof setTimeout> | null = null
  let hideTimer: ReturnType<typeof setTimeout> | null = null
  let pointerX = 0
  let pointerY = 0

  function clearShow(): void {
    if (showTimer) {
      clearTimeout(showTimer)
      showTimer = null
    }
  }

  function clearHide(): void {
    if (hideTimer) {
      clearTimeout(hideTimer)
      hideTimer = null
    }
  }

  function hide(): void {
    clearShow()
    clearHide()
    visible.value = false
    row.value = null
  }

  function enabled(): boolean {
    return options?.enabled ? options.enabled() : true
  }

  function onRowEnter(target: TRow, e: MouseEvent): void {
    pointerX = e.clientX
    pointerY = e.clientY
    clearHide()
    if (!enabled()) {
      return
    }
    if (visible.value) {
      // 已显示：切行即时跟随（重新按当前光标定位）
      row.value = target
      x.value = pointerX
      y.value = pointerY
      return
    }
    clearShow()
    showTimer = setTimeout(() => {
      showTimer = null
      if (!enabled()) {
        return
      }
      row.value = target
      x.value = pointerX
      y.value = pointerY
      visible.value = true
    }, delay)
  }

  function onRowLeave(): void {
    clearShow()
    // 行间缝隙宽限：100ms 内进入相邻行则不隐藏
    clearHide()
    hideTimer = setTimeout(hide, 100)
  }

  function rowProps(target: TRow): Record<string, unknown> {
    return {
      onMouseenter: (e: MouseEvent) => onRowEnter(target, e),
      onMousemove: (e: MouseEvent) => {
        pointerX = e.clientX
        pointerY = e.clientY
      },
      onMouseleave: onRowLeave,
    }
  }

  // 滚动（捕获表格内部滚动）/ 按下 / 键盘 → 即时隐藏
  const onInterrupt = () => hide()
  window.addEventListener('scroll', onInterrupt, true)
  window.addEventListener('pointerdown', onInterrupt, true)
  window.addEventListener('keydown', onInterrupt, true)

  onBeforeUnmount(() => {
    hide()
    window.removeEventListener('scroll', onInterrupt, true)
    window.removeEventListener('pointerdown', onInterrupt, true)
    window.removeEventListener('keydown', onInterrupt, true)
  })

  return { visible, row, x, y, rowProps, hide }
}
