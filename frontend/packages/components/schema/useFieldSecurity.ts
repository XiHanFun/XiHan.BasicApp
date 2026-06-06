import type { MaybeRefOrGetter } from 'vue'
import { useAppContext } from '~/stores'
import { ref, toValue } from 'vue'

/**
 * 字段权限（FLS）前端读取。
 *
 * 字段脱敏已由服务端在响应里落地（前端不再二次打码）；此 hook 供页面表单按 `isEditable`
 * 置只读，或展示「不可见 / 已脱敏」标识。按页面 resourceCode 拉取当前用户的有效字段规则。
 */
export interface FieldSecurityRule {
  fieldName: string
  isReadable: boolean
  isEditable: boolean
  maskStrategy: number
  maskPattern?: null | string
}

export interface UseFieldSecurity {
  /** 取字段规则（无规则返回 undefined） */
  ruleFor: (fieldKey: string) => FieldSecurityRule | undefined
  /** 字段是否可读（默认 true） */
  isReadable: (fieldKey: string) => boolean
  /** 字段是否可编辑（默认 true；用于表单只读 / 提交校验） */
  isEditable: (fieldKey: string) => boolean
  /** 拉取并缓存当前用户在该资源上的字段规则 */
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
      // 端点未就绪 / 无规则
    }
  }

  const ruleFor = (fieldKey: string): FieldSecurityRule | undefined => rules.value[fieldKey]
  const isReadable = (fieldKey: string): boolean => rules.value[fieldKey]?.isReadable ?? true
  const isEditable = (fieldKey: string): boolean => rules.value[fieldKey]?.isEditable ?? true

  return { ruleFor, isReadable, isEditable, resolve }
}
