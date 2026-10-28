import type { Tone } from '@xihan-ui/core'
import { XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { h } from 'vue'

/**
 * 列表单元格里的状态标签（SchemaPage 字段的 render）。
 * 4.x 的状态列经 dictionaryCode 取后端枚举元数据并按 status-tag 表配色；6.0 后端还没有枚举元数据接口，
 * 页面按契约字面量自己给文案与语气，观感与 4.x 相同。
 */
export function renderTag(label: string, tone?: Tone) {
  return h(XhTagRoot, { variant: 'subtle', size: 'sm', tone }, () => h(XhTagLabel, null, () => label))
}
