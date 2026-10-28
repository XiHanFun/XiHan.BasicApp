/** 授权穿梭框条目最小契约：主键即穿梭框的值 */
export interface GrantTransferItem {
  basicId: number | string
}

/** 穿梭框的一段；name 为空时不出段标题 */
export interface GrantTransferGroup<T extends GrantTransferItem> {
  key: string
  name: string
  items: T[]
}

/** 条目所在的一侧：source 可授、target 已授 */
export type GrantTransferSide = 'source' | 'target'
