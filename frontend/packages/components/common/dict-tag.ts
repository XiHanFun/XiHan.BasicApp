/** 字典标签的公共显示契约：使用公开色槽增强辨识度，以图形补充颜色，未知值保留原码。 */
import type { VNodeChild } from 'vue'
import type { DictTagStyle } from '~/types/dictionary'
import { XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { h } from 'vue'
import DictTagIcon from './DictTagIcon.vue'

export type { DictTagStyle } from '~/types/dictionary'

// 成功保持较浅的绿底，信息采用更明显的蓝底；明度与图形同时区分，避免只依赖红绿色觉。
// 前景向正文色收敛，防止应用主题中偏亮的成功原色降低文字对比度。
// 颜色全部来自语义令牌，随浅/深色主题变化；这份只读预设不涉及共享可变状态或网络请求。
const readablePresets = {
  success: {
    '--xh-tag-bg': 'color-mix(in oklab, var(--xh-fg-success) 18%, var(--xh-bg-surface))',
    '--xh-tag-fg': 'color-mix(in oklab, var(--xh-fg-success) 35%, var(--xh-fg-default))',
  },
  info: {
    '--xh-tag-bg': 'color-mix(in oklab, var(--xh-fg-info) 38%, var(--xh-bg-surface))',
    '--xh-tag-fg': 'color-mix(in oklab, var(--xh-fg-info) 35%, var(--xh-fg-default))',
  },
} as const

/** @param style 字典显示配置。 @returns 关闭时为null，否则优先自定义图标，再按配色取默认；不驱动业务状态。 */
export function dictTagIcon(style?: DictTagStyle | null): string | null {
  if (style?.showIcon === false)
    return null
  if (style?.icon && style.icon.length <= 160 && /^(?:carbon|ep|heroicons|lucide|mdi|simple-icons|tabler):[a-z0-9]+(?:-[a-z0-9]+)*$/.test(style.icon))
    return style.icon
  switch (style?.tone) {
    case 'success': return 'lucide:check'
    case 'info': return 'lucide:info'
    case 'brand': return 'lucide:hourglass'
    case 'warning': return 'lucide:triangle-alert'
    case 'danger': return 'lucide:x'
    default: return 'lucide:circle'
  }
}

/** @param style 服务端样式。 @returns 预设使用语义令牌，人工配置只接受六位颜色字面量并优先覆盖。 */
export function dictTagCss(style?: DictTagStyle | null): Record<string, string> {
  const output: Record<string, string> = style?.tone === 'success' || style?.tone === 'info' ? { ...readablePresets[style.tone] } : {}
  if (style?.backgroundColor && /^#[\dA-F]{6}$/i.test(style.backgroundColor))
    output['--xh-tag-bg'] = style.backgroundColor
  if (style?.textColor && /^#[\dA-F]{6}$/i.test(style.textColor))
    output['--xh-tag-fg'] = style.textColor
  return output
}

/** @param text 字典名称或未知原码。 @param style 显示配置。 @param description 可选中文说明。 @returns 安全转义的标准标签，不包含业务判断。 */
export function renderDictTag(text: string, style?: DictTagStyle | null, description?: string | null): VNodeChild {
  const icon = dictTagIcon(style)
  return h(XhTagRoot, { variant: 'subtle', tone: style?.tone ?? 'neutral', style: dictTagCss(style), title: description ?? undefined }, () => [
    ...(icon ? [h(DictTagIcon, { 'icon': icon, 'class': 'shrink-0', 'aria-hidden': true })] : []),
    h(XhTagLabel, () => text),
  ])
}
