import type { SchemaQueryParams, SchemaResource } from '~/components'
import type { ResponseOf } from '~/contracts'
import { computed, reactive, ref } from 'vue'
import { configs as configsApi, toPageRequest, toSchemaPageResult } from '~/contracts'
import { ApiError } from '~/request'

export type ConfigRow = ResponseOf<'configs.list'>['items'][number]
export type ConfigDataType = ConfigRow['dataType']
export type ConfigStatus = ConfigRow['status']

export const CONFIG_DATA_TYPES: readonly ConfigDataType[] = ['Text', 'Number', 'Boolean', 'Json', 'Array']

/** 弹窗表单：新建、编辑与覆盖平台参数共用一份（键只在新建时可改） */
export interface ConfigForm {
  key: string
  name: string
  group: string
  dataType: ConfigDataType
  value: string
  defaultValue: string
  isEncrypted: boolean
  allowTenantOverride: boolean
  order: number
  description: string
  status: ConfigStatus
}

export type ConfigEditorMode = 'create' | 'edit' | 'override'

function blankToNull(value: string): null | string {
  return value.trim() === '' ? null : value
}

function emptyForm(): ConfigForm {
  return { key: '', name: '', group: '', dataType: 'Text', value: '', defaultValue: '', isEncrypted: false, allowTenantOverride: true, order: 0, description: '', status: 'Active' }
}

/** 批量删除的结果里只有失败项的错误码：逐项拒绝的原因拼成一条报错 */
function failedCodes(result: { failed: { code: string }[] }): void {
  if (result.failed.length > 0)
    throw new Error(result.failed.map(f => f.code).join('; '))
}

/**
 * SchemaPage 的资源：分页（读共享：平台参数 + 本侧参数）、删除、启停与导入新建。
 * 搜索字段按后端白名单下发：分组 / 数据类型 / 所属 / 状态 / 是否加密精确匹配，关键字匹配键与名称。
 */
export function configsResource(): SchemaResource<ConfigRow> {
  return {
    page: async (params: SchemaQueryParams) => toSchemaPageResult(
      await configsApi.list(toPageRequest(params, { filters: { group: 'eq', dataType: 'eq', owner: 'eq', status: 'eq', isEncrypted: 'eq' } })),
      params,
    ),
    remove: async id => failedCodes(await configsApi.delete({ ids: [String(id)] })),
    // 更新是整体覆盖：先取当前值再只改状态；加密参数的值传空即保留原值
    updateStatus: async (id, enabled) => {
      const row = await configsApi.get({ id: String(id) })
      return configsApi.update({
        id: row.id,
        name: row.name,
        group: row.group,
        dataType: row.dataType,
        value: row.isEncrypted ? null : row.value,
        defaultValue: row.defaultValue,
        isEncrypted: row.isEncrypted,
        allowTenantOverride: row.allowTenantOverride,
        order: row.order,
        description: row.description,
        status: enabled ? 'Active' : 'Disabled',
      })
    },
    // 导入：importable 字段组装的记录 → 新建请求（缺省值在此兜底）
    create: record => configsApi.create({
      key: String(record.key ?? '').trim(),
      name: String(record.name ?? '').trim(),
      group: blankToNull(String(record.group ?? '')),
      dataType: (record.dataType as ConfigDataType | undefined) ?? 'Text',
      value: blankToNull(String(record.value ?? '')),
      defaultValue: blankToNull(String(record.defaultValue ?? '')),
      isEncrypted: false,
      allowTenantOverride: true,
      order: typeof record.order === 'number' ? record.order : 0,
      description: blankToNull(String(record.description ?? '')),
    }),
  }
}

/**
 * 参数页的编辑弹窗（两侧都有）：新建（租户同键即覆盖平台参数）、编辑本侧参数、按平台参数预填覆盖。
 * 另一侧的参数只读；加密参数不回显值，编辑时值留空即保留原值。字段级错误挂回对应字段。
 */
export function useConfigEditor(isPlatformCaller: () => boolean) {
  const open = ref(false)
  const mode = ref<ConfigEditorMode>('create')
  const editing = ref<ConfigRow | null>(null)
  const form = reactive<ConfigForm>(emptyForm())
  const fieldErrors = ref<Record<string, string[]>>({})
  const submitError = ref<string | null>(null)
  const submitting = ref(false)

  /** 这行参数是否属于调用方这一侧（只有本侧的能改、能删） */
  function isOwn(row: ConfigRow): boolean {
    return row.isPlatform === isPlatformCaller()
  }

  /** 租户可以覆盖允许覆盖的平台参数 */
  function canOverride(row: ConfigRow): boolean {
    return !isPlatformCaller() && row.isPlatform && row.allowTenantOverride
  }

  const isEdit = computed(() => mode.value === 'edit')

  function reset(next: Partial<ConfigForm> = {}) {
    Object.assign(form, emptyForm(), next)
    fieldErrors.value = {}
    submitError.value = null
  }

  function startCreate() {
    mode.value = 'create'
    editing.value = null
    reset()
    open.value = true
  }

  function startEdit(row: ConfigRow) {
    mode.value = 'edit'
    editing.value = row
    reset({
      key: row.key,
      name: row.name,
      group: row.group ?? '',
      dataType: row.dataType,
      value: row.isEncrypted ? '' : (row.value ?? ''),
      defaultValue: row.defaultValue ?? '',
      isEncrypted: row.isEncrypted,
      allowTenantOverride: row.allowTenantOverride,
      order: row.order,
      description: row.description ?? '',
      status: row.status,
    })
    open.value = true
  }

  /** 租户覆盖平台参数：用平台参数的键、名称、分组与数据类型预填新建表单 */
  function startOverride(row: ConfigRow) {
    mode.value = 'override'
    editing.value = null
    reset({ key: row.key, name: row.name, group: row.group ?? '', dataType: row.dataType, allowTenantOverride: false })
    open.value = true
  }

  function fieldError(field: keyof ConfigForm): string | undefined {
    return fieldErrors.value[field]?.[0]
  }

  async function submit(): Promise<boolean> {
    submitting.value = true
    submitError.value = null
    fieldErrors.value = {}
    try {
      const current = editing.value
      if (mode.value === 'edit' && current) {
        await configsApi.update({
          id: current.id,
          name: form.name.trim(),
          group: blankToNull(form.group),
          dataType: form.dataType,
          value: blankToNull(form.value),
          defaultValue: blankToNull(form.defaultValue),
          isEncrypted: form.isEncrypted,
          allowTenantOverride: form.allowTenantOverride,
          order: form.order,
          description: blankToNull(form.description),
          status: form.status,
        })
      }
      else {
        await configsApi.create({
          key: form.key.trim(),
          name: form.name.trim(),
          group: blankToNull(form.group),
          dataType: form.dataType,
          value: blankToNull(form.value),
          defaultValue: blankToNull(form.defaultValue),
          isEncrypted: form.isEncrypted,
          allowTenantOverride: form.allowTenantOverride,
          order: form.order,
          description: blankToNull(form.description),
        })
      }
      open.value = false
      return true
    }
    catch (error) {
      if (error instanceof ApiError) {
        fieldErrors.value = error.errors ?? {}
        if (!error.hasFieldErrors)
          submitError.value = error.detail
      }
      else {
        submitError.value = String(error)
      }
      return false
    }
    finally {
      submitting.value = false
    }
  }

  return { open, mode, isEdit, editing, form, fieldError, submitError, submitting, isOwn, canOverride, startCreate, startEdit, startOverride, submit }
}
