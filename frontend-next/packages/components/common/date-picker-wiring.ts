/**
 * 日期选择封装的字段接线：id 与 aria-* 落在 role=group 的段位组上。
 *
 * Control 只是画描边的视觉盒，没有角色，接线挂在它上面读屏读不到；日历钮（Trigger）
 * 的字段标签与校验状态由组件库自己接，这里只管段位组。
 * 段位组按自己的值算 data-disabled / data-invalid（越界也算不合法），
 * 字段那一份没成立时是 undefined，盖上去会把这些判定抹掉，所以不转交。
 */
const GROUP_OWN_STATE = new Set(['data-disabled', 'data-readonly', 'data-invalid'])

/** 只随名字一起转交给区间终点那组的状态接线 */
const END_GROUP_WIRING = ['aria-describedby', 'aria-invalid', 'aria-required', 'aria-readonly'] as const

/**
 * 单日选择与区间起点那组的接线。
 * keepOwnName 为真时，名字链末尾接上组自己的 id：组自带的 aria-label（区间的「开始日期」）
 * 跟在字段标签后面一起读，而不是被字段标签整个顶掉。
 */
export function segmentGroupWiring(controlAttrs: Readonly<Record<string, unknown>>, keepOwnName = false): Record<string, unknown> {
  const wiring: Record<string, unknown> = {}
  for (const [key, value] of Object.entries(controlAttrs)) {
    if (!GROUP_OWN_STATE.has(key)) {
      wiring[key] = value
    }
  }
  const id = wiring.id
  const labelledBy = wiring['aria-labelledby']
  if (keepOwnName && typeof id === 'string' && id && typeof labelledBy === 'string' && labelledBy) {
    wiring['aria-labelledby'] = `${labelledBy} ${id}`
  }
  return wiring
}

/**
 * 区间终点那组的接线：id 由起点那份派生（字段只发一个 id），名字链同样接上组自己的「结束日期」，
 * 说明与校验状态照抄。不在字段里、也没有外来 id 时什么都不挂，组件库自己的那份原样生效。
 */
export function endSegmentGroupWiring(controlAttrs: Readonly<Record<string, unknown>>): Record<string, unknown> {
  const id = controlAttrs.id
  if (typeof id !== 'string' || !id) {
    return {}
  }
  const endId = `${id}-end`
  const wiring: Record<string, unknown> = { id: endId }
  for (const key of END_GROUP_WIRING) {
    if (controlAttrs[key] !== undefined) {
      wiring[key] = controlAttrs[key]
    }
  }
  const labelledBy = controlAttrs['aria-labelledby']
  if (typeof labelledBy === 'string' && labelledBy) {
    wiring['aria-labelledby'] = `${labelledBy} ${endId}`
  }
  return wiring
}
