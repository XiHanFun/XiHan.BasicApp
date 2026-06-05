import type { MaybeRefOrGetter } from 'vue'
import { useAppContext } from '~/stores'
import { ref, toValue } from 'vue'

/**
 * 字段脱敏（对接后端 FLS）。
 *
 * 按页面 resourceCode 拉取当前用户的有效脱敏规则，转成 `fls:*` formatter 注入字段，
 * 复用渲染器既有脱敏管线。无规则 / 端点未就绪时静默不脱敏（安全降级）。
 */
export interface FieldSecurityRule {
  fieldName: string
  isReadable: boolean
  maskStrategy: number
  maskPattern?: null | string
}

export interface UseFieldSecurity {
  /** 字段对应的脱敏 formatter（注入 field.formatter；无规则返回 undefined） */
  formatterFor: (fieldKey: string) => string | undefined
  /** 拉取并缓存当前用户在该资源上的脱敏规则 */
  resolve: () => Promise<void>
}

export function useFieldSecurity(resourceCode: MaybeRefOrGetter<string | undefined>): UseFieldSecurity {
  const api = useAppContext().apis.fieldSecurityApi
  const rules = ref<Record<string, FieldSecurityRule>>({})

  async function resolve(): Promise<void> {
    const code = toValue(resourceCode)
    if (!code) {
      return
    }
    try {
      const list = await api.getMine(code)
      const map: Record<string, FieldSecurityRule> = {}
      for (const rule of list) {
        map[rule.fieldName] = rule
      }
      rules.value = map
    }
    catch {
      // 端点未就绪 / 无规则 → 不脱敏
    }
  }

  function formatterFor(fieldKey: string): string | undefined {
    const rule = rules.value[fieldKey]
    if (!rule) {
      return undefined
    }
    if (!rule.isReadable) {
      return 'fls:hide'
    }
    if (rule.maskStrategy === 2) {
      return 'fls:full'
    }
    if (rule.maskStrategy !== 0) {
      return 'fls:partial'
    }
    return undefined
  }

  return { formatterFor, resolve }
}
