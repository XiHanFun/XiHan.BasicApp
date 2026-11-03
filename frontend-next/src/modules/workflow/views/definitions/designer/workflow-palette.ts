/**
 * 设计器物料里能放的节点类型：工作流引擎预设照 4.x 引擎列了 17 种活动，6.0 后端跑的是框架 5 的引擎
 *
 * - Script 框架 5 没有（W5），恒不放；
 * - 后端的活动类型目录（workflow-definitions.activity-types）取到后，只留目录里有的
 *
 * 只收窄预设的节点类型，属性、规则、编辑方式与文案原样；查看器不收窄，存量定义里的节点照样画得出来
 */
import type { createXihanWorkflow } from '~/designers/flow'

type WorkflowPreset = ReturnType<typeof createXihanWorkflow>

/** 框架 5 没有的活动类型 */
export const UNSUPPORTED_ACTIVITY_TYPES: ReadonlySet<string> = new Set(['Script'])

/**
 * 物料分组的展示名：后端活动类型目录里的分类展示名（CategoryName，已按界面语言翻译），键是分类标识的小写，
 * 与预设的分组标识一致；目录里没有的分组（如预设的 event）沿用预设的文案
 * @param items 后端活动类型目录
 * @returns 分组标识 → 展示名
 */
export function paletteCategoryNames(items: readonly { category?: string | null, categoryName?: string | null }[]): Record<string, string> {
  const names: Record<string, string> = {}
  for (const item of items) {
    if (item.category && item.categoryName)
      names[item.category.toLowerCase()] = item.categoryName
  }
  return names
}

/**
 * 按后端能跑的活动类型收窄预设的节点类型
 * @param preset 工作流引擎预设的扩展
 * @param allowed 后端活动类型目录里的类型名；还没取到或取不到时不传，只去掉框架 5 没有的类型
 */
export function restrictActivityTypes(preset: WorkflowPreset, allowed?: ReadonlySet<string>): WorkflowPreset {
  return {
    ...preset,
    nodeTypes: preset.nodeTypes?.filter(nodeType => !UNSUPPORTED_ACTIVITY_TYPES.has(nodeType.type) && (!allowed || allowed.has(nodeType.type))),
  }
}
