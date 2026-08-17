import type { MenuNode } from '@xihan-ui/headless'
import type { AppDropdownOption } from '~/types'

/**
 * 把应用侧的下拉条目摊成组件库的 collection。
 *
 * 两处形状差异在这里收口：
 * 1. 分隔线。应用侧沿用「独立一条 `{ key, type: 'divider' }`」的写法（全站几十处调用点如此），
 *    组件库那侧是「下一条上带 separatorBefore」。这里把独立分隔条折进它后面那条真条目；
 *    落在末尾的分隔条直接丢掉——菜单最后一根线没有意义。
 * 2. 首条上的分隔线。组件库明确不产出它，这里也就不必特意抹平。
 */
export function toDropdownCollection(options: ReadonlyArray<AppDropdownOption>): MenuNode[] {
  const out: MenuNode[] = []
  let pendingSeparator = false

  for (const option of options) {
    if (option.type === 'divider') {
      pendingSeparator = true
      continue
    }
    out.push({
      value: option.key,
      label: typeof option.label === 'string' ? option.label : option.key,
      ...(option.disabled ? { disabled: true } : {}),
      ...(pendingSeparator || option.divider ? { separatorBefore: true } : {}),
    })
    pendingSeparator = false
  }

  return out
}

/** 按 key 建索引，供条目插槽回查图标与富标签 */
export function indexDropdownOptions(
  options: ReadonlyArray<AppDropdownOption>,
): Map<string, AppDropdownOption> {
  return new Map(options.filter(o => o.type !== 'divider').map(o => [o.key, o]))
}
