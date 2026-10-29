import type { SchemaQueryParams, SchemaResource } from '~/components'
import type { PageQueryMapping, RequestOf, ResponseOf } from '~/contracts'
import { storageConfigs as storageConfigsApi, toPageRequest, toSchemaPageResult } from '~/contracts'

export type StorageConfigRow = ResponseOf<'storage-configs.list'>['items'][number]
export type StorageType = StorageConfigRow['type']
export type StorageConfigStatus = StorageConfigRow['status']

type Translate = (key: string, values?: Record<string, unknown>) => string

export const STORAGE_TYPES: readonly StorageType[] = ['Local', 'S3', 'Oss', 'Cos', 'MinIO']
export const STORAGE_CONFIG_STATUSES: readonly StorageConfigStatus[] = ['Active', 'Disabled']

/** 列表的后端筛选：关键字匹配编码、名称、桶与备注；类型、状态多选（合成 in），是否缺省按 true / false 精确匹配 */
export const STORAGE_CONFIG_QUERY: PageQueryMapping = {
  multiples: ['type', 'status'],
  filters: { isDefault: 'eq' },
}

/** 桶名规则（与后端一致）：3 到 63 个小写字母、数字或连字符，首尾是字母或数字；本地存储是根目录下的子目录名 */
export const BUCKET_PATTERN = /^[a-z0-9][a-z0-9-]{1,61}[a-z0-9]$/

/** 远程（S3 兼容）存储要服务地址与访问密钥；本地存储只用桶 */
export function isRemote(type: StorageType): boolean {
  return type !== 'Local'
}

/** 编辑弹窗的表单 */
export interface StorageConfigForm {
  id: null | string
  code: string
  name: string
  type: StorageType
  bucket: string
  endpoint: string
  region: string
  accessKeyId: string
  /** 新的访问密钥；编辑时留空即保留原值 */
  secretAccessKey: string
  isDefault: boolean
  status: StorageConfigStatus
  order: null | number
  remark: string
}

export function emptyStorageConfigForm(): StorageConfigForm {
  return { id: null, code: '', name: '', type: 'Local', bucket: '', endpoint: '', region: '', accessKeyId: '', secretAccessKey: '', isDefault: false, status: 'Active', order: 100, remark: '' }
}

/** 已保存的配置回填表单（访问密钥不回显） */
export function toStorageConfigForm(row: StorageConfigRow): StorageConfigForm {
  return {
    id: row.id,
    code: row.code,
    name: row.name,
    type: row.type,
    bucket: row.bucket,
    endpoint: row.endpoint ?? '',
    region: row.region ?? '',
    accessKeyId: row.accessKeyId ?? '',
    secretAccessKey: '',
    isDefault: row.isDefault,
    status: row.status,
    order: row.order,
    remark: row.remark ?? '',
  }
}

/** 表单的第一处问题（4.x 的提交前校验加上 6.0 的桶名与地址规则）；都好为空 */
export type StorageConfigProblem = 'access_key_id' | 'bucket' | 'code' | 'endpoint' | 'name' | 'secret'

/**
 * @param form 表单
 * @param hasSecret 编辑的配置是否已存了访问密钥（已存的留空即保留）
 */
export function findConfigProblem(form: StorageConfigForm, hasSecret: boolean): StorageConfigProblem | null {
  const code = form.code.trim()
  if (!form.id && (!code || /\s/.test(code)))
    return 'code'
  if (!form.name.trim())
    return 'name'
  if (!BUCKET_PATTERN.test(form.bucket.trim()))
    return 'bucket'
  if (isRemote(form.type)) {
    if (!isHttpUrl(form.endpoint.trim()))
      return 'endpoint'
    if (!form.accessKeyId.trim())
      return 'access_key_id'
    if (!form.secretAccessKey.trim() && !(form.id && hasSecret))
      return 'secret'
  }
  return null
}

function isHttpUrl(value: string): boolean {
  try {
    const url = new URL(value)
    return url.protocol === 'http:' || url.protocol === 'https:'
  }
  catch {
    return false
  }
}

function blankToNull(value: string): null | string {
  return value.trim() || null
}

/** 远程设置：本地存储一律不带（后端也会清掉） */
function remoteSettings(form: StorageConfigForm) {
  const remote = isRemote(form.type)
  return {
    endpoint: remote ? blankToNull(form.endpoint) : null,
    region: remote ? blankToNull(form.region) : null,
    accessKeyId: remote ? blankToNull(form.accessKeyId) : null,
    secretAccessKey: remote ? blankToNull(form.secretAccessKey) : null,
  }
}

export function toCreateBody(form: StorageConfigForm): RequestOf<'storage-configs.create'> {
  return {
    code: form.code.trim(),
    name: form.name.trim(),
    type: form.type,
    bucket: form.bucket.trim(),
    ...remoteSettings(form),
    // 停用的配置不能设为缺省
    isDefault: form.status === 'Active' && form.isDefault,
    status: form.status,
    order: form.order ?? 0,
    remark: blankToNull(form.remark),
  }
}

/** 编辑：编码不改；访问密钥留空即保留原值 */
export function toUpdateBody(form: StorageConfigForm & { id: string }): RequestOf<'storage-configs.update'> {
  return {
    id: form.id,
    name: form.name.trim(),
    type: form.type,
    bucket: form.bucket.trim(),
    ...remoteSettings(form),
    order: form.order ?? 0,
    remark: blankToNull(form.remark),
  }
}

/** 批量删除只回失败项的错误码：认得的翻成文案，其余原样 */
export function batchFailureMessage(t: Translate, code: string): string {
  switch (code) {
    case 'FILES_NOTFOUND_002': return t('files.storage_configs.batch.not_found')
    case 'FILES_CONFLICT_002': return t('files.storage_configs.batch.default_locked')
    case 'FILES_CONFLICT_003': return t('files.storage_configs.batch.in_use')
    default: return code
  }
}

/**
 * SchemaPage 的资源：分页（缺省配置在前）、删除、启停。
 * 删除逐项拒绝的原因（缺省配置、还有副本）拼成一条报错。
 */
export function storageConfigResource(t: Translate): SchemaResource<StorageConfigRow> {
  return {
    page: async (params: SchemaQueryParams) => toSchemaPageResult(await storageConfigsApi.list(toPageRequest(params, STORAGE_CONFIG_QUERY)), params),
    remove: async (id) => {
      const result = await storageConfigsApi.delete({ ids: [String(id)] })
      if (result.failed.length > 0)
        throw new Error(result.failed.map(failure => batchFailureMessage(t, failure.code)).join('; '))
    },
    updateStatus: async (id, enabled) => storageConfigsApi.setStatus({ id: String(id), status: enabled ? 'Active' : 'Disabled' }),
  }
}

// 枚举值是契约字面量，文案键静态写出以便 i18n 门禁核对

export function storageTypeLabel(t: Translate, type: StorageType): string {
  switch (type) {
    case 'S3': return t('files.storage_configs.types.s3')
    case 'Oss': return t('files.storage_configs.types.oss')
    case 'Cos': return t('files.storage_configs.types.cos')
    case 'MinIO': return t('files.storage_configs.types.minio')
    default: return t('files.storage_configs.types.local')
  }
}

export function storageConfigStatusLabel(t: Translate, status: StorageConfigStatus): string {
  return status === 'Disabled' ? t('files.storage_configs.statuses.disabled') : t('files.storage_configs.statuses.active')
}

export function configProblemMessage(t: Translate, problem: StorageConfigProblem): string {
  switch (problem) {
    case 'code': return t('files.storage_configs.problems.code')
    case 'name': return t('files.storage_configs.problems.name')
    case 'bucket': return t('files.storage_configs.problems.bucket')
    case 'endpoint': return t('files.storage_configs.problems.endpoint')
    case 'access_key_id': return t('files.storage_configs.problems.access_key_id')
    case 'secret': return t('files.storage_configs.problems.secret')
  }
}
