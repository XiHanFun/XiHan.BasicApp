import type { SchemaQueryParams, SchemaResource } from '~/components'
import type { RequestOf, ResponseOf } from '~/contracts'
import { computed, reactive, ref } from 'vue'
import { departments as departmentsApi, fieldSecurity as fieldSecurityApi, roles as rolesApi, toPageRequest, toSchemaPageResult, users as usersApi } from '~/contracts'
import { ApiError } from '~/request'

export type FieldRuleRow = ResponseOf<'field-security.list'>['items'][number]
export type FieldSecurityEntity = ResponseOf<'field-security.entities'>['items'][number]
export type FieldMaskStrategy = FieldRuleRow['strategy']
export type FieldTargetKind = FieldRuleRow['targetKind']
export type FieldRuleStatus = FieldRuleRow['status']
type DepartmentNode = ResponseOf<'departments.tree'>['items'][number]

export const FIELD_MASK_STRATEGIES: readonly FieldMaskStrategy[] = ['None', 'Hidden', 'FullMask', 'PartialMask', 'Hash', 'Redact']
export const FIELD_TARGET_KINDS: readonly FieldTargetKind[] = ['Role', 'User', 'Department']

/** 部分脱敏保留位数上限，与后端一致 */
export const MAX_MASK_KEEP = 32

/** 组合框候选：value 一律是字符串 */
export interface TargetOption {
  value: string
  label: string
  description?: string
}

/** 弹窗表单（新建与编辑共用；4.x 的缺省值：隐藏、前 3 后 4） */
export interface FieldRuleForm {
  targetKind: FieldTargetKind
  targetId: string
  entity: string
  field: string
  strategy: FieldMaskStrategy
  keepHead: number
  keepTail: number
  replacement: string
  isEditable: boolean
  status: FieldRuleStatus
  remark: string
}

function emptyForm(): FieldRuleForm {
  return { targetKind: 'Role', targetId: '', entity: '', field: '', strategy: 'Hidden', keepHead: 3, keepTail: 4, replacement: '', isEditable: false, status: 'Active', remark: '' }
}

/** 批量删除的结果里只有失败项的错误码：逐项拒绝的原因拼成一条报错 */
function failedCodes(result: { failed: { code: string }[] }): void {
  if (result.failed.length > 0)
    throw new Error(result.failed.map(f => f.code).join('; '))
}

/** 只提交当前读取方式用得到的参数（4.x 的 buildDefinition）；明文规则一律只读 */
export function toDefinition(form: FieldRuleForm): Omit<RequestOf<'field-security.create'>, 'status'> {
  const partial = form.strategy === 'PartialMask'
  return {
    targetKind: form.targetKind,
    targetId: form.targetId,
    entity: form.entity,
    field: form.field,
    strategy: form.strategy,
    keepHead: partial ? form.keepHead : null,
    keepTail: partial ? form.keepTail : null,
    replacement: form.strategy === 'Redact' ? (form.replacement.trim() || null) : null,
    isEditable: form.strategy !== 'None' && form.isEditable,
    remark: form.remark.trim() || null,
  }
}

/** 一条规则的整体修改请求：启停也走它（后端是整体覆盖） */
function updateRequest(row: FieldRuleRow, status: FieldRuleStatus): RequestOf<'field-security.update'> {
  return { id: row.id, targetKind: row.targetKind, targetId: row.targetId, entity: row.entity, field: row.field, strategy: row.strategy, keepHead: row.keepHead, keepTail: row.keepTail, replacement: row.replacement, isEditable: row.isEditable, status, remark: row.remark }
}

/**
 * SchemaPage 的资源：分页（作用侧 = 当前租户或平台）、删除、启停。
 * 搜索按后端白名单：关键字匹配字段名与备注；实体、目标类型、读取方式、状态多选（合成 in）。
 */
export function fieldSecurityResource(): SchemaResource<FieldRuleRow> {
  return {
    page: async (params: SchemaQueryParams) => toSchemaPageResult(
      await fieldSecurityApi.list(toPageRequest(params, { multiples: ['entity', 'targetKind', 'strategy', 'status'] })),
      params,
    ),
    remove: async id => failedCodes(await fieldSecurityApi.delete({ ids: [String(id)] })),
    updateStatus: async (id, enabled) => {
      const row = await fieldSecurityApi.get({ id: String(id) })
      return fieldSecurityApi.update(updateRequest(row, enabled ? 'Active' : 'Disabled'))
    },
  }
}

/** 部门树展平成候选：副文本是从根到它的路径；停用的部门不能做目标，不列出（下级照常列） */
export function flattenDepartments(nodes: readonly DepartmentNode[], path: string[] = []): TargetOption[] {
  return nodes.flatMap((node) => {
    const trail = [...path, node.department.name]
    const self = node.department.status === 'Active' ? [{ value: node.department.id, label: node.department.name, description: trail.join(' / ') }] : []
    return [...self, ...flattenDepartments(node.children, trail)]
  })
}

/** 消息树拍平成键（只要文本叶子），给字段显示名的兜底查找用 */
export function messageKeys(messages: unknown, prefix = ''): string[] {
  if (!messages || typeof messages !== 'object')
    return []
  return Object.entries(messages as Record<string, unknown>).flatMap(([key, value]) => {
    const path = prefix ? `${prefix}.${key}` : key
    return typeof value === 'string' ? [path] : messageKeys(value, path)
  })
}

/** 通用字段词表的文案键前缀（创建时间、备注、排序这类各页共有的字段） */
export const COMMON_FIELD_PREFIX = 'access.field_security.common_fields.'

function snakeOf(name: string): string {
  return name.replace(/[A-Z]/g, letter => `_${letter.toLowerCase()}`)
}

/**
 * 字段显示名的文案键：登记的键 → 同一页面命名空间（键的前两段）里末段是字段名（或 col_ / detail_ / form_ 加字段名）的键
 * → 通用字段词表 → 没有（显示字段名）。字段名是接口的 camelCase，文案键末段是 snake_case。
 */
export function fieldTitleKey(titleKey: string, name: string, keys: readonly string[]): string | null {
  const known = new Set(keys)
  if (known.has(titleKey))
    return titleKey
  const snake = snakeOf(name)
  const namespace = `${titleKey.split('.').slice(0, 2).join('.')}.`
  const tails = new Set([snake, `col_${snake}`, `detail_${snake}`, `form_${snake}`])
  const sibling = keys.find(key => key.startsWith(namespace) && tails.has(key.slice(key.lastIndexOf('.') + 1)))
  if (sibling)
    return sibling
  const common = `${COMMON_FIELD_PREFIX}${snake}`
  return known.has(common) ? common : null
}

/** 实体目录：实体、字段下拉的唯一来源，页面打开时取一次 */
export function useFieldSecurityEntities() {
  const entities = ref<FieldSecurityEntity[]>([])
  const loading = ref(false)

  async function load() {
    loading.value = true
    try {
      entities.value = [...(await fieldSecurityApi.entities({})).items]
    }
    finally {
      loading.value = false
    }
  }

  function find(code: string): FieldSecurityEntity | undefined {
    return entities.value.find(entity => entity.code === code)
  }

  return { entities, loading, load, find }
}

/**
 * 字段规则的编辑弹窗（4.x 的字段安全表单）：目标按类型远程检索，实体与字段从目录选；
 * 非文本字段只能明文或隐藏、不可空的字段不能隐藏，明文规则只能只读。字段级错误挂回对应字段。
 */
export function useFieldRuleEditor(findEntity: (code: string) => FieldSecurityEntity | undefined) {
  const open = ref(false)
  const editing = ref<FieldRuleRow | null>(null)
  const form = reactive<FieldRuleForm>(emptyForm())
  const fieldErrors = ref<Record<string, string[]>>({})
  const submitError = ref<string | null>(null)
  const submitting = ref(false)
  const targetOptions = ref<TargetOption[]>([])
  const targetLoading = ref(false)
  let targetSeq = 0

  const isEdit = computed(() => editing.value !== null)
  const currentEntity = computed(() => findEntity(form.entity))
  const currentField = computed(() => currentEntity.value?.fields.find(field => field.name === form.field))
  const isMasked = computed(() => form.strategy !== 'None')

  /** 当前字段能用的读取方式：非文本只能明文或隐藏；不可空的不能隐藏 */
  function strategyAllowed(strategy: FieldMaskStrategy): boolean {
    const field = currentField.value
    if (!field)
      return true
    if (!field.isText && strategy !== 'None' && strategy !== 'Hidden')
      return false
    return field.isNullable || strategy !== 'Hidden'
  }

  function reset(next: Partial<FieldRuleForm> = {}) {
    Object.assign(form, emptyForm(), next)
    fieldErrors.value = {}
    submitError.value = null
  }

  /** 只认最后一次检索的结果，免得慢请求把新结果盖掉 */
  async function searchTargets(keyword: string) {
    const seq = ++targetSeq
    targetLoading.value = true
    try {
      const kw = keyword.trim() || null
      let next: TargetOption[]
      switch (form.targetKind) {
        case 'User': {
          const result = await usersApi.list({ page: { index: 1, size: 50 }, filters: [], sorts: [], keyword: kw })
          next = result.items.map(user => ({ value: user.id, label: user.displayName, description: `@${user.userName}` }))
          break
        }
        case 'Department': {
          const tree = await departmentsApi.tree({})
          const all = flattenDepartments(tree.items)
          next = kw ? all.filter(option => option.label.toLowerCase().includes(kw.toLowerCase()) || option.description?.toLowerCase().includes(kw.toLowerCase())) : all
          break
        }
        default: {
          const result = await rolesApi.list({ page: { index: 1, size: 50 }, filters: [{ field: 'status', operator: 'eq', value: 'Active' }], sorts: [], keyword: kw })
          next = result.items.map(role => ({ value: role.id, label: role.name, description: role.code }))
          break
        }
      }
      if (seq === targetSeq)
        targetOptions.value = next
    }
    finally {
      if (seq === targetSeq)
        targetLoading.value = false
    }
  }

  function startCreate() {
    editing.value = null
    reset()
    targetOptions.value = []
    open.value = true
    return searchTargets('')
  }

  function startEdit(row: FieldRuleRow) {
    editing.value = row
    reset({
      targetKind: row.targetKind,
      targetId: row.targetId,
      entity: row.entity,
      field: row.field,
      strategy: row.strategy,
      keepHead: row.keepHead ?? 3,
      keepTail: row.keepTail ?? 4,
      replacement: row.replacement ?? '',
      isEditable: row.isEditable,
      status: row.status,
      remark: row.remark ?? '',
    })
    // 先放入当前目标一条：远程候选还没回来时输入框也念得出它的名字
    targetOptions.value = [{ value: row.targetId, label: row.targetName ?? row.targetId }]
    open.value = true
  }

  // 联动只响应用户操作：打开编辑时整体回填表单，不能被联动清掉目标与字段
  function changeTargetKind(kind: FieldTargetKind) {
    if (kind === form.targetKind)
      return
    form.targetKind = kind
    form.targetId = ''
    targetOptions.value = []
    void searchTargets('')
  }

  function changeEntity(code: string) {
    if (code === form.entity)
      return
    form.entity = code
    form.field = ''
  }

  /** 选中的字段不再适用当前读取方式时，退回隐藏或明文 */
  function changeField(name: string) {
    form.field = name
    if (!strategyAllowed(form.strategy))
      changeStrategy(strategyAllowed('Hidden') ? 'Hidden' : 'None')
  }

  /** 明文规则只能是只读，否则什么都没限制 */
  function changeStrategy(strategy: FieldMaskStrategy) {
    form.strategy = strategy
    if (strategy === 'None')
      form.isEditable = false
  }

  function fieldError(field: keyof FieldRuleForm): string | undefined {
    return fieldErrors.value[field]?.[0]
  }

  async function submit(): Promise<boolean> {
    submitting.value = true
    submitError.value = null
    fieldErrors.value = {}
    try {
      const current = editing.value
      if (current)
        await fieldSecurityApi.update({ id: current.id, ...toDefinition(form), status: form.status })
      else
        await fieldSecurityApi.create({ ...toDefinition(form), status: form.status })
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

  return { open, isEdit, editing, form, fieldError, submitError, submitting, targetOptions, targetLoading, currentEntity, currentField, isMasked, strategyAllowed, searchTargets, startCreate, startEdit, changeTargetKind, changeEntity, changeField, changeStrategy, submit }
}
