import type { Translate } from '../jobs/job'
import type { RequestOf, ResponseOf } from '~/contracts'
import { ref } from 'vue'
import { caches as cachesApi } from '~/contracts'
import { ApiError } from '~/request'

/** 一个开放管理的缓存键命名空间（各包登记的前缀，如 access:） */
export type CacheNamespace = ResponseOf<'caches.namespaces'>['items'][number]
/** 一个缓存项：大小、剩余存活、滑动过期与按 UTF-8 解出的文本 */
export type CacheEntry = ResponseOf<'caches.inspect'>
/** 批量删除里没删成的一个键与原因码 */
export type CacheDeleteFailure = ResponseOf<'caches.delete'>['failed'][number]

/** 每页取多少个键（后端上限 1000）；4.x 一次取全部，6.0 按续页标记往下翻 */
export const KEY_PAGE_SIZE = 500

/** 一次 caches.delete 最多几个键（后端上限 100），多选超出时分批发 */
export const DELETE_BATCH_SIZE = 100

/** 键已过期或不存在 */
export const ENTRY_NOT_FOUND = 'MAINTENANCE_NOTFOUND_003'

/** 键不在开放的命名空间里 */
export const NAMESPACE_DENIED = 'MAINTENANCE_DENIED_002'

/** 按序数比较（与后端 StringComparer.Ordinal 一致），不随区域排序规则变 */
function compareOrdinal(a: string, b: string): number {
  return a < b ? -1 : a > b ? 1 : 0
}

// ── 命名空间 ────────────────────────────────────────────────────

/** 包名的显示文本：本应用登记了命名空间的包写出文案（键静态写出以便 i18n 门禁核对），其余原样显示包名 */
export function packageLabel(t: Translate, name: string): string {
  switch (name) {
    case 'access': return t('maintenance.caches.packages.access')
    case 'identity': return t('maintenance.caches.packages.identity')
    case 'notification': return t('maintenance.caches.packages.notification')
    case 'openplatform': return t('maintenance.caches.packages.openplatform')
    case 'tenancy': return t('maintenance.caches.packages.tenancy')
    default: return name
  }
}

/** 命名空间下拉的选项：值是前缀，文本是「前缀 · 包名」，鉴权相关（不可改写）的标上只读 */
export function namespaceOptions(t: Translate, namespaces: readonly CacheNamespace[]): { label: string, value: string }[] {
  return namespaces.map((namespace) => {
    const values = { prefix: namespace.prefix, name: packageLabel(t, namespace.package) }
    return {
      value: namespace.prefix,
      label: namespace.editable ? t('maintenance.caches.namespace_option', values) : t('maintenance.caches.namespace_option_read_only', values),
    }
  })
}

/** 键或前缀所在的命名空间（与后端同口径：以命名空间前缀开头）；不在任何开放命名空间里为 null */
export function namespaceOf(namespaces: readonly CacheNamespace[], keyOrPrefix: string): CacheNamespace | null {
  return namespaces.find(namespace => keyOrPrefix.startsWith(namespace.prefix)) ?? null
}

/** 取开放的命名空间（按前缀排序） */
export async function loadNamespaces(): Promise<CacheNamespace[]> {
  return (await cachesApi.namespaces({})).items
}

// ── 键列表 ──────────────────────────────────────────────────────

/**
 * 查询的完整前缀：命名空间前缀加上子前缀（去首尾空白）。
 * 6.0 只认前缀、不认通配：4.x 的键模式 `basicapp:*` 在这里是选命名空间、再按需补一段子前缀。
 */
export function keyPrefix(namespacePrefix: string, subPrefix: string): string {
  return `${namespacePrefix}${subPrefix.trim()}`
}

/** caches.keys 的请求：第一页不带续页标记 */
export function toKeysRequest(prefix: string, continuation: null | string): RequestOf<'caches.keys'> {
  return continuation ? { prefix, limit: KEY_PAGE_SIZE, continuation } : { prefix, limit: KEY_PAGE_SIZE }
}

/** 并入下一页的键：Redis 的 SCAN 游标可能把同一个键排进两页，跨页去重后按序数排序（4.x 同样排序后建树） */
export function mergeKeys(existing: readonly string[], incoming: readonly string[]): string[] {
  return [...new Set([...existing, ...incoming])].sort(compareOrdinal)
}

/**
 * 键列表的状态：按前缀取第一页、再按续页标记往下翻，跨页去重。
 * 换前缀或重查时作废在途的上一笔，晚到的旧响应不覆盖新结果，也不报错。
 */
export function useCacheKeys() {
  const keys = ref<string[]>([])
  /** 下一页的续页标记；没有下一页为 null */
  const continuation = ref<null | string>(null)
  /** 当前列表对应的前缀；还没查过为 null */
  const prefix = ref<null | string>(null)
  const loading = ref(false)
  /** 在途的是「加载更多」：页面据此不给整棵树罩加载层 */
  const appending = ref(false)
  let generation = 0

  async function fetchPage(at: string, token: null | string, append: boolean): Promise<boolean> {
    const current = ++generation
    loading.value = true
    appending.value = append
    try {
      const page = await cachesApi.keys(toKeysRequest(at, token))
      if (current !== generation)
        return false
      prefix.value = at
      keys.value = mergeKeys(append ? keys.value : [], page.items)
      continuation.value = page.continuation ?? null
      return true
    }
    catch (error) {
      if (current !== generation)
        return false
      throw error
    }
    finally {
      if (current === generation) {
        loading.value = false
        appending.value = false
      }
    }
  }

  /** 按前缀重查第一页；返回 false 表示被更新的一次查询作废 */
  function load(at: string): Promise<boolean> {
    return fetchPage(at, null, false)
  }

  /** 取下一页并入列表；没有下一页或正在查询时不发请求 */
  async function loadMore(): Promise<boolean> {
    if (prefix.value === null || continuation.value === null || loading.value)
      return false
    return fetchPage(prefix.value, continuation.value, true)
  }

  /** 从列表里拿掉已删掉（或本来就不在了）的键，续页标记不动 */
  function discard(gone: readonly string[]): void {
    const set = new Set(gone)
    keys.value = keys.value.filter(key => !set.has(key))
  }

  return { keys, continuation, prefix, loading, appending, load, loadMore, discard }
}

// ── 键树（4.x 原样：按 : 分组） ─────────────────────────────────

/** 分组节点的键前缀：选中回调据此只取叶子，避免多选时混入分组 */
export const GROUP_PREFIX = '__group__'

export interface CacheTreeOption {
  value: string
  label: string
  /** 分组节点的叶子数；叶子节点没有 */
  leafCount?: number
  children?: CacheTreeOption[]
}

/**
 * 扁平键 → 树（按 : 分组）。叶子与分组分槽存储：
 * 同一路径既是某键又是更深键前缀（如 a:b 与 a:b:c 并存）时不互相覆盖、不丢键。
 */
export function buildKeyTree(keys: readonly string[]): CacheTreeOption[] {
  const root: CacheTreeOption[] = []
  const groups = new Map<string, CacheTreeOption>()
  const leaves = new Set<string>()

  for (const key of keys) {
    const parts = key.split(':')
    let siblings = root
    let path = ''
    for (let i = 0; i < parts.length - 1; i++) {
      const segment = parts[i]!
      path = path ? `${path}:${segment}` : segment
      let group = groups.get(path)
      if (!group) {
        group = { value: `${GROUP_PREFIX}${path}`, label: segment, children: [], leafCount: 0 }
        groups.set(path, group)
        siblings.push(group)
      }
      group.leafCount = (group.leafCount ?? 0) + 1
      siblings = group.children!
    }
    if (!leaves.has(key)) {
      leaves.add(key)
      siblings.push({ value: key, label: parts[parts.length - 1]! })
    }
  }
  return root
}

/** 全部分组节点的键（键少时展开整棵树用） */
export function collectGroupKeys(nodes: readonly CacheTreeOption[]): string[] {
  const keys: string[] = []
  const walk = (list: readonly CacheTreeOption[]) => {
    for (const node of list) {
      if (node.children?.length) {
        keys.push(node.value)
        walk(node.children)
      }
    }
  }
  walk(nodes)
  return keys
}

/** 选中集合里的叶子（真正的缓存键） */
export function leafKeys(selected: readonly string[]): string[] {
  return selected.filter(key => !key.startsWith(GROUP_PREFIX))
}

/** 不超过这么多键时整棵树展开（4.x 同为 100） */
export const EXPAND_ALL_LIMIT = 100

// ── 缓存项 ──────────────────────────────────────────────────────

/** caches.inspect 的数值是 int64，统一按 Number 读 */
function seconds(value: null | number | undefined): null | number {
  return value === null || value === undefined ? null : Number(value)
}

/** 字节大小（4.x 同）：不足 1 KB 按 B，不足 1 MB 按 KB 保留一位，否则按 MB 保留两位 */
export function formatSize(bytes: number): string {
  const size = Number(bytes)
  if (size < 1024)
    return `${size} B`
  if (size < 1024 * 1024)
    return `${(size / 1024).toFixed(1)} KB`
  return `${(size / 1024 / 1024).toFixed(2)} MB`
}

/** 秒数的可读写法：取最大的非零单位，再带上紧接的下一级（为零则不带），如「1 小时 5 分」「2 天」「45 秒」 */
export function formatSeconds(t: Translate, value: number): string {
  const total = Math.max(0, Math.round(value))
  const units = [
    { amount: Math.floor(total / 86400), label: (n: number) => t('maintenance.caches.units.day', { n }) },
    { amount: Math.floor((total % 86400) / 3600), label: (n: number) => t('maintenance.caches.units.hour', { n }) },
    { amount: Math.floor((total % 3600) / 60), label: (n: number) => t('maintenance.caches.units.minute', { n }) },
    { amount: total % 60, label: (n: number) => t('maintenance.caches.units.second', { n }) },
  ]
  const first = units.findIndex(unit => unit.amount > 0)
  if (first < 0)
    return units[3]!.label(0)
  const parts = [units[first]!.label(units[first]!.amount)]
  const next = units[first + 1]
  if (next && next.amount > 0)
    parts.push(next.label(next.amount))
  return parts.join(' ')
}

/** 剩余存活：不过期为「永不过期」 */
export function timeToLiveText(t: Translate, entry: Pick<CacheEntry, 'timeToLiveSeconds'>): string {
  const value = seconds(entry.timeToLiveSeconds)
  return value === null ? t('maintenance.caches.never_expire') : formatSeconds(t, value)
}

/** 滑动过期：没有为「无」 */
export function slidingText(t: Translate, entry: Pick<CacheEntry, 'slidingSeconds'>): string {
  const value = seconds(entry.slidingSeconds)
  return value === null ? t('maintenance.caches.no_sliding') : formatSeconds(t, value)
}

/** 能否改写：有改写权限，且后端判定可改（命名空间可改写、值是文本） */
export function canEditEntry(entry: Pick<CacheEntry, 'editable' | 'text'> | null, canUpdate: boolean): boolean {
  return canUpdate && !!entry && entry.editable && entry.text !== null && entry.text !== undefined
}

/**
 * 不能改写的原因：值不是 UTF-8 文本（binary，不能预览也不能改）；或命名空间鉴权相关只许删（read-only，4.x 同样禁止改写）。
 * 能改写时为 null。
 */
export function readOnlyReason(entry: Pick<CacheEntry, 'editable' | 'text'>): 'binary' | 'read-only' | null {
  if (entry.text === null || entry.text === undefined)
    return 'binary'
  return entry.editable ? null : 'read-only'
}

/** 值是不是 JSON 对象或数组（4.x 同：据此缺省切到 Json 视图） */
export function isJson(text: string): boolean {
  const trimmed = text.trim()
  if (!trimmed || (!trimmed.startsWith('{') && !trimmed.startsWith('[')))
    return false
  try {
    JSON.parse(trimmed)
    return true
  }
  catch {
    return false
  }
}

/** Json 视图的展示值：美化缩进；不是合法 JSON 时原样 */
export function prettyJson(text: string): string {
  try {
    return JSON.stringify(JSON.parse(text), null, 2)
  }
  catch {
    return text
  }
}

/**
 * 当前查看的缓存项。
 * 键不存在或已过期（MAINTENANCE_NOTFOUND_003）不算错误，只把 entry 置空、由页面显示「键不存在」；其余错误抛给调用方。
 * 连点几个键时只认最后一次，晚到的旧响应丢弃。
 */
export function useCacheEntry() {
  const key = ref<null | string>(null)
  const entry = ref<CacheEntry | null>(null)
  const loading = ref(false)
  let generation = 0

  async function open(next: string): Promise<void> {
    const current = ++generation
    key.value = next
    entry.value = null
    loading.value = true
    try {
      const value = await cachesApi.inspect({ key: next })
      if (current === generation)
        entry.value = value
    }
    catch (error) {
      if (current !== generation)
        return
      if (error instanceof ApiError && error.code === ENTRY_NOT_FOUND)
        return
      throw error
    }
    finally {
      if (current === generation)
        loading.value = false
    }
  }

  /** 重新取当前键 */
  async function reload(): Promise<void> {
    if (key.value !== null)
      await open(key.value)
  }

  /** 改写当前键的值（过期设置沿用原项），成功后以返回的缓存项为准 */
  async function save(value: string): Promise<void> {
    if (key.value === null)
      return
    const at = key.value
    const updated = await cachesApi.update({ key: at, value })
    if (key.value === at)
      entry.value = updated
  }

  function reset(): void {
    generation++
    key.value = null
    entry.value = null
    loading.value = false
  }

  return { key, entry, loading, open, reload, save, reset }
}

// ── 删除 ────────────────────────────────────────────────────────

/** 删除的结果：删掉的、本来就不在的（已过期，后端不算删掉）与没删成的 */
export interface CacheDeleteOutcome {
  removed: string[]
  missing: string[]
  failed: CacheDeleteFailure[]
}

/** 去重后每 100 个一批 */
export function toDeleteBatches(keys: readonly string[]): string[][] {
  const unique = [...new Set(keys)]
  const batches: string[][] = []
  for (let i = 0; i < unique.length; i += DELETE_BATCH_SIZE)
    batches.push(unique.slice(i, i + DELETE_BATCH_SIZE))
  return batches
}

/** 按键删除（超过 100 个分批发）；既不在删掉里、也不在失败里的键是本来就不在了 */
export async function deleteKeys(keys: readonly string[]): Promise<CacheDeleteOutcome> {
  const removed: string[] = []
  const failed: CacheDeleteFailure[] = []
  const batches = toDeleteBatches(keys)
  for (const batch of batches) {
    const result = await cachesApi.delete({ keys: batch })
    removed.push(...result.removed)
    failed.push(...result.failed)
  }
  const settled = new Set([...removed, ...failed.map(failure => failure.key)])
  return { removed, failed, missing: batches.flat().filter(key => !settled.has(key)) }
}

/** 已经不在缓存里的键（删掉的加本来就不在的）：列表里一并拿掉 */
export function goneKeys(outcome: CacheDeleteOutcome): string[] {
  return [...outcome.removed, ...outcome.missing]
}

/** 没删成的原因码：认得的翻成文案，其余原样 */
export function deleteFailureMessage(t: Translate, code: string): string {
  return code === NAMESPACE_DENIED ? t('maintenance.caches.namespace_denied') : code
}

/** 结果提示：语气与文案 */
export interface CacheToast {
  tone: 'danger' | 'success' | 'warning'
  message: string
}

/** 删除当前键的提示：删掉即成功；本来就不在了（已过期）提醒一句；没删成报原因 */
export function singleDeleteToast(t: Translate, outcome: CacheDeleteOutcome): CacheToast {
  const failure = outcome.failed[0]
  if (failure)
    return { tone: 'danger', message: deleteFailureMessage(t, failure.code) }
  if (outcome.removed.length === 0)
    return { tone: 'warning', message: t('maintenance.caches.key_already_gone') }
  return { tone: 'success', message: t('common.messages.delete_success') }
}

/** 批量删除的提示（4.x 同口径）：全部不在了即成功（其中已过期的单独说一句）；有没删成的提示成功与失败各几个，并附原因 */
export function batchDeleteToast(t: Translate, outcome: CacheDeleteOutcome): CacheToast {
  if (outcome.failed.length > 0) {
    const reason = [...new Set(outcome.failed.map(failure => deleteFailureMessage(t, failure.code)))].join('; ')
    return {
      tone: 'warning',
      message: t('maintenance.caches.batch_delete_partial', { success: goneKeys(outcome).length, failed: outcome.failed.length, reason }),
    }
  }
  if (outcome.missing.length > 0)
    return { tone: 'success', message: t('maintenance.caches.batch_deleted_with_missing', { count: outcome.removed.length, missing: outcome.missing.length }) }
  return { tone: 'success', message: t('maintenance.caches.batch_deleted', { count: outcome.removed.length }) }
}

/** 按前缀删除（前缀须落在开放的命名空间里，不支持通配），返回删掉的键数 */
export async function deleteByPrefix(prefix: string): Promise<number> {
  return Number((await cachesApi.deleteByPrefix({ prefix })).removed)
}
