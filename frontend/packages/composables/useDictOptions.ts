import type { ComputedRef, MaybeRefOrGetter } from 'vue'
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

function reasonOf(error: unknown): string {
  return error instanceof Error && error.message ? error.message : String(error)
}

/**
 * 取字典选项（带缓存与并发去重）。
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
    const request = useAppContext().apis.dictApi.options(code)
    pending = request
      .then((items) => {
        const options = items.map(item => ({
          label: item.label,
          value: item.value,
          parentValue: item.parentValue ?? null,
          isDefault: item.isDefault,
          disabled: item.disabled,
        }))
        cache.value = { ...cache.value, [code]: options }
        return options
      })
      .catch((error: unknown) => {
        toast.danger(i18n.global.t('component.dict_options.load_failed', { code, reason: reasonOf(error) }))
        throw error
      })
      .finally(() => {
        inflight.delete(code)
      })
    inflight.set(code, pending)
  }
  return pending
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
