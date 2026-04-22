import type { PageQuery } from '~/types'
import { useBaseApi } from '../base'
import { buildPageRequest, normalizePageResult, toId, toNumber, unwrapPayload } from '../helpers'

const api = useBaseApi('FieldLevelSecurity')

export interface SysFieldLevelSecurity {
  basicId: string
  tenantId?: string
  targetType: number
  targetId: string
  resourceId: string
  fieldName: string
  isReadable: boolean
  isEditable: boolean
  maskStrategy: number
  maskPattern?: string
  priority: number
  description?: string
  status: number
  createTime?: string
  updateTime?: string
  remark?: string
}

export interface FieldLevelSecurityPageQuery extends PageQuery {
  status?: number
  targetType?: number
  resourceId?: string
}

const STATUS_MAP: Record<string, number> = { Yes: 1, No: 0 }

function resolveEnum(value: unknown, map: Record<string, number>, fallback: number): number {
  if (value === undefined || value === null)
    return fallback
  if (typeof value === 'number')
    return value
  if (typeof value === 'string')
    return map[value] ?? toNumber(value, fallback)
  return fallback
}

function resolveBool(value: unknown, fallback: boolean): boolean {
  if (value === undefined || value === null)
    return fallback
  if (typeof value === 'boolean')
    return value
  if (typeof value === 'number')
    return value !== 0
  if (typeof value === 'string') {
    const normalized = value.trim().toLowerCase()
    if (normalized === 'true' || normalized === '1' || normalized === 'yes')
      return true
    if (normalized === 'false' || normalized === '0' || normalized === 'no')
      return false
  }
  return fallback
}

function normalizeRule(raw: Record<string, any>): SysFieldLevelSecurity {
  return {
    basicId: toId(raw.basicId ?? raw.BasicId),
    tenantId: toId(raw.tenantId ?? raw.TenantId) || undefined,
    targetType: toNumber(raw.targetType ?? raw.TargetType, 0),
    targetId: toId(raw.targetId ?? raw.TargetId),
    resourceId: toId(raw.resourceId ?? raw.ResourceId),
    fieldName: raw.fieldName ?? raw.FieldName ?? '',
    isReadable: resolveBool(raw.isReadable ?? raw.IsReadable, true),
    isEditable: resolveBool(raw.isEditable ?? raw.IsEditable, true),
    maskStrategy: toNumber(raw.maskStrategy ?? raw.MaskStrategy, 0),
    maskPattern: raw.maskPattern ?? raw.MaskPattern ?? undefined,
    priority: toNumber(raw.priority ?? raw.Priority, 0),
    description: raw.description ?? raw.Description ?? undefined,
    status: resolveEnum(raw.status ?? raw.Status, STATUS_MAP, 1),
    createTime: raw.createTime ?? raw.creationTime ?? raw.createdTime ?? raw.CreatedTime ?? undefined,
    updateTime: raw.updateTime ?? raw.lastModificationTime ?? raw.modifiedTime ?? raw.ModifiedTime ?? undefined,
    remark: raw.remark ?? raw.Remark ?? undefined,
  }
}

function toCreatePayload(data: Partial<SysFieldLevelSecurity>) {
  return {
    tenantId: data.tenantId ? toId(data.tenantId) : null,
    targetType: toNumber(data.targetType, 0),
    targetId: toId(data.targetId ?? ''),
    resourceId: toId(data.resourceId ?? ''),
    fieldName: (data.fieldName ?? '').trim(),
    isReadable: data.isReadable !== false,
    isEditable: data.isEditable !== false,
    maskStrategy: toNumber(data.maskStrategy, 0),
    maskPattern: (data.maskPattern ?? '').trim(),
    priority: toNumber(data.priority, 0),
    description: (data.description ?? '').trim(),
    remark: (data.remark ?? '').trim(),
  }
}

function toUpdatePayload(id: string, data: Partial<SysFieldLevelSecurity>) {
  return {
    ...toCreatePayload(data),
    status: toNumber(data.status, 1),
    basicId: toId(id),
  }
}

const PAGE_OPTIONS = {
  keywordFields: ['FieldName', 'Description', 'MaskPattern'],
  filterFieldMap: { status: 'Status', targetType: 'TargetType', resourceId: 'ResourceId' },
}

async function queryPage(params: Record<string, any>) {
  const data = await api.request.post<any>(`${api.baseUrl}Page`, buildPageRequest(params, PAGE_OPTIONS))
  return normalizePageResult(data, normalizeRule)
}

export const fieldLevelSecurityApi = {
  page: (params: Record<string, any>) => queryPage(params),
  detail: (id: string) =>
    api.detail(id).then(raw => normalizeRule((unwrapPayload<any>(raw) ?? {}) as Record<string, any>)),
  create: (data: Partial<SysFieldLevelSecurity>) => api.create(toCreatePayload(data)),
  update: (id: string, data: Partial<SysFieldLevelSecurity>) =>
    api.request.put(`${api.baseUrl}Update`, toUpdatePayload(id, data), { params: { id } }),
  delete: (id: string) => api.deletePath(id),
}

export function getFieldLevelSecurityPageApi(params: FieldLevelSecurityPageQuery) {
  return queryPage(params as Record<string, any>)
}
