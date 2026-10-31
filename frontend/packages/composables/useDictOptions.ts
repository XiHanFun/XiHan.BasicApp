/** 字典选项缓存：并发合并、样式透传和保存后失效；旧在途响应不能覆盖新配置。 */
import type { ComputedRef, MaybeRefOrGetter } from 'vue'
import type { DictTagStyle } from '../components/common/dict-tag'
import { computed, ref, toValue, watch } from 'vue'
import { i18n } from '~/locales'
import { useAppContext } from '~/stores/app-context'
import { toast } from './ui-service'

/**
 * 系统字典选项（字典管理里维护的字典，区别于后端枚举）。
 *
 * 值为字典项编码，业务数据按它存；树形字典按深度优先展平，parentValue 指向上级项编码。
 */
export interface DictOption {
  description?: string
  tagStyle?: DictTagStyle | null
  label: string
  value: string
  /** 树形字典的上级项编码（顶层为 null） */
  parentValue: null | string
  isDefault: boolean
  /** 停用项：只用于回显引用了它的历史数据，下拉里不能再选 */
  disabled: boolean
}

/** 模块级缓存：同一会话内按字典编码只取一次，表单下拉、列表单元格与搜索框共用 */
const cache = ref<Record<string, DictOption[]>>({})
const inflight = new Map<string, Promise<DictOption[]>>()
const generations = new Map<string, number>()
const channel = typeof window === 'undefined' || typeof window.BroadcastChannel === 'undefined' ? null : new window.BroadcastChannel('xihan-dictionary-display')
if (channel) {
  channel.onmessage = (event) => {
    if (typeof event.data === 'string' && event.data.length <= 100)
      void invalidateDictOptions(event.data, false).catch(() => { /* ensureDictOptions已展示读取失败 */ })
  }
}
if (import.meta.hot)
  import.meta.hot.dispose(() => channel?.close())

function reasonOf(error: unknown): string {
  return error instanceof Error && error.message ? error.message : String(error)
}

/**
 * 取字典选项（带缓存与并发去重）。
 * @param dictCode 字典编码。
 * @returns 选项快照。
 * @throws 读取失败时透传请求错误，调用者不能使用旧缓存冒充新配置。
 *
 * 失败时就地提示一次再抛出：字典编码写错或字典被删时，空下拉会让问题无声无息。
 * 失败不进缓存，下次调用会重取。
 */
export function ensureDictOptions(dictCode: string): Promise<DictOption[]> {
  const code = dictCode.trim()
  if (!code) {
    return Promise.resolve([])
  }
  const cached = cache.value[code]
  if (cached) {
    return Promise.resolve(cached)
  }
  let pending = inflight.get(code)
  if (!pending) {
    const generation = generations.get(code) ?? 0
    const request = useAppContext().apis.dictApi.options(code)
    pending = request
      .then((items) => {
        const options = items.map(item => ({
          label: item.label,
          description: item.description ?? undefined,
          value: item.value,
          parentValue: item.parentValue ?? null,
          isDefault: item.isDefault,
          disabled: item.disabled,
          tagStyle: item.tagStyle,
        }))
        if ((generations.get(code) ?? 0) !== generation)
          return ensureDictOptions(code)
        cache.value = { ...cache.value, [code]: options }
        return options
      })
      .catch((error: unknown) => {
        if ((generations.get(code) ?? 0) !== generation)
          return ensureDictOptions(code)
        toast.danger(i18n.global.t('component.dict_options.load_failed', { code, reason: reasonOf(error) }))
        throw error
      })
      .finally(() => {
        if (inflight.get(code) === pending)
          inflight.delete(code)
      })
    inflight.set(code, pending)
  }
  return pending
}

/** @param dictCode 修改的字典。 @param broadcast 是否通知同源窗口。 @returns 刷新已使用的缓存；失败原样抛出，不能显示旧配置冒充新值。 */
export async function invalidateDictOptions(dictCode: string, broadcast = true): Promise<void> {
  const code = dictCode.trim()
  if (!code)
    return
  const loaded = code in cache.value || inflight.has(code)
  generations.set(code, (generations.get(code) ?? 0) + 1)
  const next = { ...cache.value }
  delete next[code]
  cache.value = next
  inflight.delete(code)
  if (broadcast)
    channel?.postMessage(code)
  if (loaded)
    await ensureDictOptions(code)
}

/** 读已加载的字典选项（响应式；未加载或加载失败时为空数组） */
export function getDictOptions(dictCode: string): DictOption[] {
  return cache.value[dictCode.trim()] ?? []
}

/**
 * 响应式字典选项：字典编码变化时自动加载，供表单下拉直接绑定。
 * 加载失败已在 ensureDictOptions 里提示，这里只防止未处理的 Promise 拒绝。
 */
export function useDictOptions(dictCode: MaybeRefOrGetter<string>): ComputedRef<DictOption[]> {
  watch(
    () => toValue(dictCode),
    (code) => {
      ensureDictOptions(code).catch(() => undefined)
    },
    { immediate: true },
  )
  return computed(() => getDictOptions(toValue(dictCode)))
}
