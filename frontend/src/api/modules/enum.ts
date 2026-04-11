import { useBaseApi } from '../base'

const api = useBaseApi('Enum')

export interface EnumOption {
  name: string
  value: number | string | boolean | object
  valueText: string
  label: string
  description: string
  theme?: string
  icon?: string
  order: number
  disabled: boolean
  source: 'enum' | 'dict' | string
  extra?: Record<string, any>
}

export interface EnumDefinition {
  enumName: string
  fullName: string
  displayName: string
  cultureName: string
  isFlags: boolean
  underlyingTypeName: string
  items: EnumOption[]
}

export interface EnumBatchQuery {
  enumNames: string[]
  language?: string
  includeHidden?: boolean
  includeDict?: boolean
  dictCodes?: string[]
  tenantId?: number
}

function normalizeOption(raw: Record<string, any>): EnumOption {
  return {
    name: raw.name ?? '',
    value: raw.value ?? raw.valueText ?? '',
    valueText: String(raw.valueText ?? raw.value ?? ''),
    label: raw.label ?? raw.description ?? raw.name ?? '',
    description: raw.description ?? raw.label ?? raw.name ?? '',
    theme: raw.theme ?? undefined,
    icon: raw.icon ?? undefined,
    order: Number(raw.order ?? 0),
    disabled: Boolean(raw.disabled ?? false),
    source: raw.source ?? 'enum',
    extra: raw.extra ?? undefined,
  }
}

function normalizeDefinition(raw: Record<string, any>): EnumDefinition {
  const items = Array.isArray(raw.items) ? raw.items.map((item: any) => normalizeOption(item ?? {})) : []
  return {
    enumName: raw.enumName ?? '',
    fullName: raw.fullName ?? '',
    displayName: raw.displayName ?? raw.enumName ?? '',
    cultureName: raw.cultureName ?? '',
    isFlags: Boolean(raw.isFlags ?? false),
    underlyingTypeName: raw.underlyingTypeName ?? '',
    items,
  }
}

export const enumApi = {
  getByName: (params: {
    enumName: string
    language?: string
    includeHidden?: boolean
    includeDict?: boolean
    dictCode?: string
    tenantId?: number
  }) =>
    api.request
      .get<Record<string, any>>(`${api.baseUrl}ByName`, { params })
      .then(raw => normalizeDefinition(raw ?? {})),

  getBatch: (input: EnumBatchQuery) =>
    api.request
      .post<Record<string, Record<string, any>>>(`${api.baseUrl}Batch`, input)
      .then((raw) => {
        const result: Record<string, EnumDefinition> = {}
        for (const [key, value] of Object.entries(raw ?? {})) {
          result[key] = normalizeDefinition(value ?? {})
        }
        return result
      }),

  getNames: () =>
    api.request.get<string[]>(`${api.baseUrl}Names`).then(list => Array.isArray(list) ? list : []),
}
