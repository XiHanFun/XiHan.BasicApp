import type { SchemaQueryParams, SchemaResource } from '~/components'
import type { ResponseOf } from '~/contracts'
import { computed, reactive, ref } from 'vue'
import { codegenDatasources, toPageRequest, toSchemaPageResult } from '~/contracts'
import { ApiError } from '~/request'

export type DatasourceRow = ResponseOf<'codegen-datasources.list'>['items'][number]
export type DatasourceDetail = ResponseOf<'codegen-datasources.get'>
export type DatabaseType = DatasourceRow['databaseType']
export type DatasourceStatus = DatasourceRow['status']

/** 数据库类型下拉（照 4.x 的五种与显示名） */
export const DATABASE_TYPE_OPTIONS: { label: string, value: DatabaseType }[] = [
  { label: 'MySQL', value: 'MySql' },
  { label: 'SQL Server', value: 'SqlServer' },
  { label: 'PostgreSQL', value: 'PostgreSql' },
  { label: 'Oracle', value: 'Oracle' },
  { label: 'SQLite', value: 'Sqlite' },
]

export function databaseTypeLabel(type: DatabaseType): string {
  return DATABASE_TYPE_OPTIONS.find(option => option.value === type)?.label ?? type
}

/** 编辑弹窗的表单：新建与编辑共用；口令与连接串编辑时留空即不改 */
export interface DatasourceForm {
  sourceName: string
  sourceDescription: string
  databaseType: DatabaseType
  host: string
  port: number
  databaseName: string
  userName: string
  password: string
  connectionString: string
  connectionTimeout: number
  isDefault: boolean
  order: number
  status: DatasourceStatus
}

export function emptyDatasourceForm(): DatasourceForm {
  return { sourceName: '', sourceDescription: '', databaseType: 'MySql', host: '', port: 3306, databaseName: '', userName: '', password: '', connectionString: '', connectionTimeout: 30, isDefault: false, order: 100, status: 'Active' }
}

function blankToNull(value: string): null | string {
  return value.trim() === '' ? null : value.trim()
}

/** 批量删除的逐项失败码：引用中的给专门的提示，其余原样拼出 */
export function failedDeleteCodes(result: { failed: { code: string }[] }): string[] {
  return result.failed.map(f => f.code)
}

/**
 * SchemaPage 的资源：分页与删除。
 * 搜索字段按后端白名单下发：数据库类型与状态多选，关键字匹配名称、描述、主机与库名。
 */
export function datasourcesResource(): SchemaResource<DatasourceRow> {
  return {
    page: async (params: SchemaQueryParams) => toSchemaPageResult(
      await codegenDatasources.list(toPageRequest(params, { multiples: ['databaseType', 'status'] })),
      params,
    ),
  }
}

/**
 * 数据源的编辑弹窗：新建与编辑（照 4.x：编辑时口令留空即不改，状态变了另调启停）。
 * 详情不回显口令与连接串，只说有没有显式连接串；字段级错误挂回对应字段。
 */
export function useDatasourceEditor() {
  const open = ref(false)
  const editing = ref<DatasourceDetail | null>(null)
  const form = reactive<DatasourceForm>(emptyDatasourceForm())
  const fieldErrors = ref<Record<string, string[]>>({})
  const submitError = ref<string | null>(null)
  const submitting = ref(false)
  const isEdit = computed(() => editing.value !== null)

  function reset(next: Partial<DatasourceForm> = {}) {
    Object.assign(form, emptyDatasourceForm(), next)
    fieldErrors.value = {}
    submitError.value = null
  }

  function startCreate() {
    editing.value = null
    reset()
    open.value = true
  }

  async function startEdit(id: string) {
    const detail = await codegenDatasources.get({ id })
    editing.value = detail
    reset({
      sourceName: detail.sourceName,
      sourceDescription: detail.sourceDescription ?? '',
      databaseType: detail.databaseType,
      host: detail.host,
      port: detail.port,
      databaseName: detail.databaseName,
      userName: detail.userName,
      connectionTimeout: detail.connectionTimeout,
      isDefault: detail.isDefault,
      order: detail.order,
      status: detail.status,
    })
    open.value = true
  }

  function fieldError(field: keyof DatasourceForm): string | undefined {
    return fieldErrors.value[field]?.[0]
  }

  async function submit(statusRemark: string): Promise<boolean> {
    submitting.value = true
    submitError.value = null
    fieldErrors.value = {}
    try {
      const current = editing.value
      const common = {
        sourceName: form.sourceName.trim(),
        sourceDescription: blankToNull(form.sourceDescription),
        databaseType: form.databaseType,
        host: form.host.trim(),
        port: form.port,
        databaseName: form.databaseName.trim(),
        userName: form.userName.trim(),
        password: blankToNull(form.password),
        connectionString: blankToNull(form.connectionString),
        connectionTimeout: form.connectionTimeout,
        isDefault: form.isDefault,
        order: form.order,
      }
      if (current) {
        const updated = await codegenDatasources.update({ id: current.id, concurrencyStamp: current.concurrencyStamp, ...common })
        // 状态变了另调启停（照 4.x），带上刚写入的并发戳
        if (updated.status !== form.status)
          await codegenDatasources.setStatus({ id: current.id, concurrencyStamp: updated.concurrencyStamp, status: form.status, remark: statusRemark })
      }
      else {
        await codegenDatasources.create({ ...common, status: form.status })
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

  return { open, editing, isEdit, form, fieldError, submitError, submitting, startCreate, startEdit, submit }
}
