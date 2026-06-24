import type { ComputedRef } from 'vue'
import { computed, onMounted, watch } from 'vue'
import { useAppStore } from '~/stores'
import { useEnumService } from './useEnumService'

export interface EnumOptionItem {
  label: string
  value: string | number
  // 兼容 naive-ui SelectBaseOption（含索引签名），使返回选项可直接绑定 NSelect :options
  [k: string]: unknown
}

/**
 * 响应式枚举下拉选项。
 *
 * 以**后端枚举元数据**为本地化单一事实源（按 X-Language 返回当前语言标签）：
 * 挂载时拉取、语言切换自动重取，元数据为空（未加载/未部署）时回退静态 fallback。
 * 返回的 computed 随语言/数据响应式更新，免刷新即切换。
 *
 * 适用于**非 SchemaPage** 的表单/弹窗下拉与展示标签；
 * SchemaPage 字段请用 `dictionaryCode`（已由 useSchemaDictionaries 统一处理）。
 *
 * @param enumName 后端枚举类型名（如 'EnableStatus' / 'PermissionType'，即 business.ts 常量所用枚举类型名）
 * @param fallback 元数据为空时的兜底静态选项（通常传 business.ts 的 *_OPTIONS 常量）
 */
export function useEnumOptions(
  enumName: string,
  fallback: ReadonlyArray<EnumOptionItem> = [],
): ComputedRef<EnumOptionItem[]> {
  const enumService = useEnumService()
  const appStore = useAppStore()

  function load() {
    void enumService.ensureEnum(enumName)
  }

  onMounted(load)
  // 语言切换后按新语言重取（enumState 仅按枚举名缓存、不含语言，不重取会残留旧语言）
  watch(() => appStore.locale, load)

  return computed<EnumOptionItem[]>(() => {
    const resolved = enumService
      .toSelectOptions(enumName)
      .filter(opt => typeof opt.value === 'string' || typeof opt.value === 'number')
      .map(opt => ({ label: opt.label, value: opt.value as string | number }))
    return resolved.length > 0 ? resolved : fallback.map(opt => ({ ...opt }))
  })
}
