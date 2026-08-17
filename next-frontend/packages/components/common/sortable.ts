import type { DragEndEvent } from '@dnd-kit/vue'
import { isSortable } from '@dnd-kit/vue/sortable'

/**
 * 从 dnd-kit 的拖拽结束事件解析「源索引 → 目标索引」。
 *
 * 乐观排序插件（OptimisticSortingPlugin，默认启用）会在拖拽过程中
 * 把 source.index 投影到最终落点，因此优先取 source.index；
 * 缺失时回退为目标项在列表中的索引。
 *
 * @param event dnd-kit dragEnd 事件
 * @param ids   当前列表的稳定标识数组（顺序与渲染一致）
 * @returns     需要移动时返回 { from, to }；取消 / 越界 / 原地不动时返回 null
 */
export function resolveSortMove(
  event: DragEndEvent,
  ids: ReadonlyArray<string | number>,
): { from: number, to: number } | null {
  const { source, target } = event.operation
  if (event.canceled || !source || !target) {
    return null
  }

  const from = ids.indexOf(source.id)
  if (from < 0) {
    return null
  }

  let to = isSortable(source) ? source.index : -1
  if (to < 0 || to >= ids.length) {
    to = ids.indexOf(target.id)
  }
  if (to < 0 || to === from) {
    return null
  }

  return { from, to }
}
