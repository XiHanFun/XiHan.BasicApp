<script setup lang="ts">
import type {
  ApiId,
  FieldLevelSecurityCreateDto,
  FieldLevelSecurityListItemDto,
  FieldLevelSecurityUpdateDto,
  FieldSecurityEntityDto,
  PageResult,
} from '@/api'
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import { XhFieldControl, XhFieldDescription, XhFieldErrorText, XhFieldLabel, XhFieldRoot, XhFormFieldGroup, XhFormRoot, XhSwitch, XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { computed, h, onMounted, ref, useId } from 'vue'
import { useI18n } from 'vue-i18n'
import {
  createPageRequest,
  departmentApi,
  EnableStatus,
  fieldLevelSecurityApi,
  FieldMaskStrategy,
  FieldSecurityTargetType,
  querySortsFromSchema,
  roleApi,
  userManagementApi,
} from '@/api'
import { FIELD_MASK_STRATEGY_OPTIONS, FIELD_SECURITY_TARGET_TYPE_OPTIONS, STATUS_OPTIONS } from '@/constants'
import { deleteConfirmText, SchemaPage, statusConfirmText, XCombobox, XEditModal, XInput, XNumberInput, XSelect } from '~/components'
import { toast } from '~/composables'
import { useEnumOptions } from '~/hooks'
import { useUserStore } from '~/stores'
import { getOptionLabel } from '~/utils'

defineOptions({ name: 'SystemFieldSecurityPage' })

const { t } = useI18n()
const userStore = useUserStore()

/** 编辑弹窗的保存钮靠这个 id 关联到表单，点它才会走整表校验 */
const editFormId = useId()

/** 部分脱敏保留位数上限，与后端一致 */
const MAX_MASK_KEEP = 32

interface FlsFormModel extends FieldLevelSecurityCreateDto {
  basicId?: ApiId
}

/** 组合框候选：value 一律是字符串 */
interface ComboOption {
  value: string
  label: string
  description?: string
}

const submitLoading = ref(false)
const modalVisible = ref(false)
const flsForm = ref<FlsFormModel>(createDefaultForm())

const maskStrategyOptions = useEnumOptions('FieldMaskStrategy', FIELD_MASK_STRATEGY_OPTIONS)
const targetTypeOptions = useEnumOptions('FieldSecurityTargetType', FIELD_SECURITY_TARGET_TYPE_OPTIONS)
const statusEnumOptions = useEnumOptions('EnableStatus', STATUS_OPTIONS)

// ── 实体目录：实体、字段下拉的唯一来源 ─────────────────────────────
const entities = ref<FieldSecurityEntityDto[]>([])
const entityLoading = ref(false)

async function loadEntities() {
  entityLoading.value = true
  try {
    entities.value = await fieldLevelSecurityApi.entities()
  }
  catch (error) {
    toast.danger((error as Error)?.message || t('identity.field_security.msg_load_entities_failed'))
  }
  finally {
    entityLoading.value = false
  }
}

onMounted(() => {
  void loadEntities()
})

const entityFilterOptions = computed(() => entities.value.map(entity => ({ label: entity.displayName, value: entity.entityName })))
const currentEntity = computed(() => entities.value.find(entity => entity.entityName === flsForm.value.entityName))
const currentField = computed(() => currentEntity.value?.fields.find(field => field.fieldName === flsForm.value.fieldName))

// 实体与字段目录一次取全，由组合框按文字与副文本在本地筛
const entityOptions = computed<ComboOption[]>(() => entities.value.map(entity => ({ value: entity.entityName, label: entity.displayName, description: entity.entityName })))
const fieldOptions = computed<ComboOption[]>(() => (currentEntity.value?.fields ?? []).map(field => ({ value: field.fieldName, label: field.displayName, description: field.fieldName })))

/** 非文本字段只能明文只读或隐藏 */
const formMaskStrategyOptions = computed(() => {
  const nonText = currentField.value && !currentField.value.isText
  return maskStrategyOptions.value.map(option => ({
    ...option,
    disabled: Boolean(nonText) && option.value !== FieldMaskStrategy.None && option.value !== FieldMaskStrategy.Hidden,
  }))
})

// ── 授权目标：按类型远程检索（防抖与输入框文字由组合框管） ─────────────
const targetOptions = ref<ComboOption[]>([])
const targetLoading = ref(false)
let targetRequestSeq = 0

function flattenDepartments(
  nodes: { basicId: ApiId, departmentName: string, departmentCode?: string | null, children?: unknown }[],
  path: string[] = [],
): ComboOption[] {
  return nodes.flatMap((node) => {
    const trail = [...path, node.departmentName]
    const children = (node.children ?? []) as typeof nodes
    return [
      { value: String(node.basicId), label: node.departmentName, description: trail.join(' / ') },
      ...flattenDepartments(children, trail),
    ]
  })
}

async function loadTargetOptions(keyword: string) {
  const seq = ++targetRequestSeq
  targetLoading.value = true
  try {
    const kw = keyword.trim() || null
    let next: ComboOption[]
    switch (flsForm.value.targetType) {
      case FieldSecurityTargetType.User: {
        const result = await userManagementApi.page({
          ...createPageRequest({ page: { pageIndex: 1, pageSize: 50 } }),
          keyword: kw ?? undefined,
        })
        next = result.items.map(user => ({ value: String(user.basicId), label: user.realName || user.nickName || user.userName, description: `@${user.userName}` }))
        break
      }
      case FieldSecurityTargetType.Department: {
        const tree = await departmentApi.tree({ limit: 500, keyword: kw ?? undefined })
        next = flattenDepartments(tree)
        break
      }
      default: {
        const roles = await roleApi.enabledList({ limit: 50, keyword: kw })
        next = roles.map(role => ({ value: String(role.basicId), label: role.roleName, description: role.roleCode }))
        break
      }
    }
    // 只认最后一次检索的结果，免得慢请求把新结果盖掉
    if (seq === targetRequestSeq) {
      targetOptions.value = next
    }
  }
  catch (error) {
    toast.danger((error as Error)?.message || t('identity.field_security.msg_load_target_failed'))
  }
  finally {
    if (seq === targetRequestSeq) {
      targetLoading.value = false
    }
  }
}

/** 候选值一律是字符串；清空时回到 0（未选） */
function onTargetChange(value: unknown) {
  flsForm.value.targetId = (typeof value === 'string' && value !== '' ? value : 0) as unknown as ApiId
}

const targetPlaceholder = computed(() => {
  switch (flsForm.value.targetType) {
    case FieldSecurityTargetType.User:
      return t('identity.field_security.ph_target_user')
    case FieldSecurityTargetType.Department:
      return t('identity.field_security.ph_target_department')
    default:
      return t('identity.field_security.ph_target_role')
  }
})

// ── 表单联动 ───────────────────────────────────────────────────
const isMasked = computed(() => flsForm.value.maskStrategy !== FieldMaskStrategy.None)
const modalTitle = computed(() => (flsForm.value.basicId ? t('identity.field_security.form_edit_title') : t('identity.field_security.form_create_title')))

// 联动只响应用户操作：打开编辑时整体回填表单，不能被联动清掉目标与字段

function onTargetTypeChange(value: unknown) {
  const next = value as FieldSecurityTargetType
  if (next === flsForm.value.targetType) {
    return
  }
  flsForm.value.targetType = next
  flsForm.value.targetId = 0 as unknown as ApiId
  targetOptions.value = []
  void loadTargetOptions('')
}

function onEntityChange(value: unknown) {
  const entityName = typeof value === 'string' ? value : ''
  if (entityName === flsForm.value.entityName) {
    return
  }
  flsForm.value.entityName = entityName
  flsForm.value.fieldName = ''
}

/** 选中非文本字段时，只保留仍然适用的读取方式 */
function onFieldChange(value: unknown) {
  const fieldName = typeof value === 'string' ? value : ''
  flsForm.value.fieldName = fieldName
  const field = currentEntity.value?.fields.find(item => item.fieldName === fieldName)
  if (field && !field.isText && flsForm.value.maskStrategy !== FieldMaskStrategy.None && flsForm.value.maskStrategy !== FieldMaskStrategy.Hidden) {
    onMaskStrategyChange(FieldMaskStrategy.Hidden)
  }
}

/** 明文规则只能是只读，否则什么都没限制 */
function onMaskStrategyChange(value: unknown) {
  flsForm.value.maskStrategy = value as FieldMaskStrategy
  if (flsForm.value.maskStrategy === FieldMaskStrategy.None) {
    flsForm.value.isEditable = false
  }
}

function createDefaultForm(): FlsFormModel {
  return {
    entityName: '',
    fieldName: '',
    isEditable: false,
    maskKeepHead: 3,
    maskKeepTail: 4,
    maskReplacement: null,
    maskStrategy: FieldMaskStrategy.Hidden,
    remark: null,
    status: EnableStatus.Enabled,
    targetId: 0 as unknown as ApiId,
    targetType: FieldSecurityTargetType.Role,
  }
}

function normalizeNullable(value?: string | null) {
  const normalized = value?.trim()
  return normalized || null
}

// ── 列表 ───────────────────────────────────────────────────────
const schemaPageRef = ref<{ reload: () => Promise<void> } | null>(null)

function reloadList() {
  void schemaPageRef.value?.reload()
}

/** 平台规则对所有租户生效，租户看得见、改不了 */
function canMaintain(row: unknown) {
  return !(row as FieldLevelSecurityListItemDto).isGlobal || (userStore.userInfo?.isPlatform ?? false)
}

/** 确认框里的规则名：实体 · 字段 */
function fieldSecurityName(row: FieldLevelSecurityListItemDto) {
  return [row.entityDisplayName || row.entityName, row.fieldDisplayName || row.fieldName].join(' · ')
}

function tag(text: string, tone: 'neutral' | 'info' | 'warning' | 'danger' | 'success') {
  return h(XhTagRoot, { variant: 'subtle', tone }, () => h(XhTagLabel, () => text))
}

function strategySummary(row: FieldLevelSecurityListItemDto) {
  const label = getOptionLabel(maskStrategyOptions.value, row.maskStrategy)
  if (row.maskStrategy === FieldMaskStrategy.PartialMask) {
    return `${label} · ${t('identity.field_security.summary_keep', { head: row.maskKeepHead ?? 0, tail: row.maskKeepTail ?? 0 })}`
  }
  if (row.maskStrategy === FieldMaskStrategy.Redact) {
    return `${label} · ${row.maskReplacement ?? ''}`
  }
  return label
}

const fields = computed<ListFieldSchema[]>(() => [
  { key: 'keyword', title: t('identity.field_security.col_keyword'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('identity.field_security.keyword_placeholder'), width: 240, order: 0 },
  {
    key: 'entityName',
    title: t('identity.field_security.col_entity'),
    dataType: 'enum',
    searchable: true,
    searchMultiple: true,
    sortable: true,
    options: entityFilterOptions.value,
    searchPlaceholder: t('identity.field_security.entity_placeholder'),
    minWidth: 150,
    order: 1,
    render: (row) => {
      const r = row as unknown as FieldLevelSecurityListItemDto
      return r.entityDisplayName
        ? h('span', { title: r.entityName }, r.entityDisplayName)
        : h('span', { class: 'fls-invalid' }, [r.entityName, ' ', tag(t('identity.field_security.invalid'), 'warning')])
    },
  },
  {
    key: 'fieldName',
    title: t('identity.field_security.col_field'),
    dataType: 'string',
    sortable: true,
    minWidth: 150,
    order: 2,
    render: (row) => {
      const r = row as unknown as FieldLevelSecurityListItemDto
      return r.fieldDisplayName
        ? h('span', { title: r.fieldName }, r.fieldDisplayName)
        : h('span', { class: 'fls-invalid' }, [r.fieldName, ' ', tag(t('identity.field_security.invalid'), 'warning')])
    },
  },
  {
    key: 'targetType',
    title: t('identity.field_security.col_target_type'),
    dataType: 'enum',
    searchable: true,
    searchMultiple: true,
    sortable: true,
    dictionaryCode: 'FieldSecurityTargetType',
    options: targetTypeOptions.value,
    searchPlaceholder: t('identity.field_security.target_type_placeholder'),
    width: 110,
    order: 3,
    render: row => getOptionLabel(targetTypeOptions.value, (row as unknown as FieldLevelSecurityListItemDto).targetType),
  },
  {
    key: 'targetName',
    title: t('identity.field_security.col_target'),
    dataType: 'string',
    minWidth: 140,
    order: 4,
    render: (row) => {
      const r = row as unknown as FieldLevelSecurityListItemDto
      return r.targetName || r.targetCode || String(r.targetId)
    },
  },
  {
    key: 'maskStrategy',
    title: t('identity.field_security.col_mask_strategy'),
    dataType: 'enum',
    searchable: true,
    searchMultiple: true,
    sortable: true,
    dictionaryCode: 'FieldMaskStrategy',
    options: maskStrategyOptions.value,
    searchPlaceholder: t('identity.field_security.mask_strategy_placeholder'),
    minWidth: 150,
    order: 5,
    render: row => strategySummary(row as unknown as FieldLevelSecurityListItemDto),
  },
  {
    key: 'isEditable',
    title: t('identity.field_security.col_editable'),
    dataType: 'boolean',
    sortable: true,
    width: 90,
    order: 6,
    render: (row) => {
      const r = row as unknown as FieldLevelSecurityListItemDto
      return r.isEditable
        ? tag(t('identity.field_security.write_only'), 'info')
        : tag(t('identity.field_security.read_only'), 'neutral')
    },
  },
  {
    key: 'isGlobal',
    title: t('identity.field_security.col_scope'),
    dataType: 'boolean',
    width: 90,
    order: 7,
    render: row => (row as unknown as FieldLevelSecurityListItemDto).isGlobal
      ? tag(t('identity.field_security.scope_global'), 'info')
      : tag(t('identity.field_security.scope_tenant'), 'neutral'),
  },
  {
    key: 'status',
    title: t('identity.field_security.col_status'),
    dataType: 'enum',
    searchable: true,
    searchMultiple: true,
    sortable: true,
    dictionaryCode: 'EnableStatus',
    options: statusEnumOptions.value,
    searchPlaceholder: t('identity.field_security.status_placeholder'),
    width: 90,
    order: 8,
  },
  { key: 'createdTime', title: t('identity.field_security.col_create_time'), dataType: 'datetime', sortable: true, minWidth: 170, order: 9 },
])

const schema = computed<PageSchema>(() => ({
  pageCode: 'identity.field-security',
  exportPermission: 'identity.field-security.export',
  pageName: t('identity.field_security.page_name'),
  batchRemovable: true,
  removePermission: 'identity.field-security.delete',
  statusPermission: 'identity.field-security.status',
  rowKey: 'basicId',
  fields: fields.value,
  resource: {
    page: (params) => {
      const { keyword } = params.filters
      return fieldLevelSecurityApi.page({
        ...createPageRequest({
          page: { pageIndex: params.page, pageSize: params.pageSize },
          // 排序 + 多选(entityName/targetType/maskStrategy/status) 等通用过滤统一走 conditions
          conditions: { sorts: querySortsFromSchema(params.sorts), filters: params.conditionFilters ?? [] },
        }),
        keyword: (keyword as string | undefined)?.trim() || undefined,
      }) as unknown as Promise<PageResult<Record<string, unknown>>>
    },
    remove: id => fieldLevelSecurityApi.delete(id),
    updateStatus: (id, enabled) => fieldLevelSecurityApi.updateStatus({ basicId: id, status: enabled ? EnableStatus.Enabled : EnableStatus.Disabled, remark: enabled ? t('identity.field_security.batch_enable_remark') : t('identity.field_security.batch_disable_remark') }),
  },
  actions: [
    { key: 'create', title: t('identity.field_security.action_create'), scope: 'page', type: 'primary', icon: 'lucide:plus', permission: 'identity.field-security.create' },
    { key: 'edit', title: t('identity.field_security.action_edit'), scope: 'row', icon: 'lucide:pencil', visible: canMaintain, permission: 'identity.field-security.update' },
    { key: 'toggle', title: t('identity.field_security.action_toggle'), scope: 'row', icon: 'lucide:power', confirm: true, confirmText: row => statusConfirmText(t, (row as unknown as FieldLevelSecurityListItemDto).status === EnableStatus.Enabled, fieldSecurityName(row as unknown as FieldLevelSecurityListItemDto)), visible: canMaintain, permission: 'identity.field-security.status' },
    { key: 'delete', title: t('identity.field_security.action_delete'), scope: 'row', icon: 'lucide:trash-2', type: 'error', confirm: true, confirmText: row => deleteConfirmText(t, fieldSecurityName(row as unknown as FieldLevelSecurityListItemDto)), visible: canMaintain, permission: 'identity.field-security.delete' },
  ],
}))

function onAction(payload: SchemaActionPayload) {
  const row = payload.row as unknown as FieldLevelSecurityListItemDto | undefined
  switch (payload.key) {
    case 'create':
      handleAdd()
      break
    case 'edit':
      if (row) {
        handleEdit(row)
      }
      break
    case 'toggle':
      if (row) {
        void handleToggleStatus(row)
      }
      break
    case 'delete':
      if (row) {
        void handleDelete(row)
      }
      break
  }
}

function handleAdd() {
  flsForm.value = createDefaultForm()
  targetOptions.value = []
  modalVisible.value = true
  void loadTargetOptions('')
}

function handleEdit(row: FieldLevelSecurityListItemDto) {
  flsForm.value = {
    basicId: row.basicId,
    entityName: row.entityName,
    fieldName: row.fieldName,
    isEditable: row.isEditable,
    maskKeepHead: row.maskKeepHead ?? 3,
    maskKeepTail: row.maskKeepTail ?? 4,
    maskReplacement: row.maskReplacement ?? null,
    maskStrategy: row.maskStrategy,
    remark: row.remark ?? null,
    status: row.status,
    targetId: row.targetId,
    targetType: row.targetType,
  }
  // 先放入当前目标一条：远程候选还没回来时输入框也念得出它的名字
  targetOptions.value = [{ value: String(row.targetId), label: row.targetName || row.targetCode || String(row.targetId), description: row.targetCode ?? undefined }]
  modalVisible.value = true
}

function validateForm() {
  const form = flsForm.value
  if (!form.targetId) {
    toast.warning(t('identity.field_security.msg_target_required'))
    return false
  }
  if (!form.entityName) {
    toast.warning(t('identity.field_security.msg_entity_required'))
    return false
  }
  if (!form.fieldName) {
    toast.warning(t('identity.field_security.msg_field_required'))
    return false
  }
  if (form.maskStrategy === FieldMaskStrategy.PartialMask) {
    if (form.maskKeepHead == null || form.maskKeepTail == null) {
      toast.warning(t('identity.field_security.msg_keep_required'))
      return false
    }
    if (form.maskKeepHead + form.maskKeepTail === 0) {
      toast.warning(t('identity.field_security.msg_keep_zero'))
      return false
    }
  }
  if (form.maskStrategy === FieldMaskStrategy.Redact && !normalizeNullable(form.maskReplacement)) {
    toast.warning(t('identity.field_security.msg_replacement_required'))
    return false
  }
  return true
}

/** 只提交当前读取方式用得到的参数 */
function buildDefinition() {
  const form = flsForm.value
  const partial = form.maskStrategy === FieldMaskStrategy.PartialMask
  return {
    entityName: form.entityName,
    fieldName: form.fieldName,
    isEditable: isMasked.value && form.isEditable,
    maskKeepHead: partial ? form.maskKeepHead : null,
    maskKeepTail: partial ? form.maskKeepTail : null,
    maskReplacement: form.maskStrategy === FieldMaskStrategy.Redact ? normalizeNullable(form.maskReplacement) : null,
    maskStrategy: form.maskStrategy,
    remark: normalizeNullable(form.remark),
    targetId: form.targetId,
    targetType: form.targetType,
  }
}

async function handleSubmit() {
  if (!validateForm()) {
    return
  }
  submitLoading.value = true
  try {
    if (flsForm.value.basicId) {
      const updateInput: FieldLevelSecurityUpdateDto = { basicId: flsForm.value.basicId, ...buildDefinition() }
      await fieldLevelSecurityApi.update(updateInput)
    }
    else {
      const createInput: FieldLevelSecurityCreateDto = { ...buildDefinition(), status: flsForm.value.status }
      await fieldLevelSecurityApi.create(createInput)
    }
    toast.success(t('common.messages.save_success'))
    modalVisible.value = false
    reloadList()
  }
  catch (error) {
    toast.danger((error as Error)?.message || t('common.messages.save_failed'))
  }
  finally {
    submitLoading.value = false
  }
}

async function handleDelete(row: FieldLevelSecurityListItemDto) {
  await fieldLevelSecurityApi.delete(row.basicId)
  toast.success(t('common.messages.delete_success'))
  reloadList()
}

async function handleToggleStatus(row: FieldLevelSecurityListItemDto) {
  await fieldLevelSecurityApi.updateStatus({
    basicId: row.basicId,
    remark: row.status === EnableStatus.Enabled ? t('identity.field_security.front_disable_remark') : t('identity.field_security.front_enable_remark'),
    status: row.status === EnableStatus.Enabled ? EnableStatus.Disabled : EnableStatus.Enabled,
  })
  toast.success(t('common.messages.status_updated'))
  reloadList()
}
</script>

<template>
  <SchemaPage ref="schemaPageRef" :schema="schema" @action="onAction">
    <XEditModal
      v-model:show="modalVisible"
      :title="modalTitle"
      :loading="submitLoading"
      :form-id="editFormId"
    >
      <XhFormRoot
        :id="editFormId"
        v-model:values="flsForm"
        validate-on="blur"
        class="xh-edit-form-grid"
        @submit="handleSubmit"
      >
        <XhFormFieldGroup name="targetType">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('identity.field_security.label_target_type') }}</XhFieldLabel>
            <XhFieldControl>
              <XSelect :value="flsForm.targetType" :options="targetTypeOptions" @update:value="onTargetTypeChange" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup name="targetId">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('identity.field_security.label_target') }}</XhFieldLabel>
            <XhFieldControl>
              <XCombobox
                class="fls-combobox"
                remote
                :options="targetOptions"
                :value="flsForm.targetId || null"
                :loading="targetLoading"
                :placeholder="targetPlaceholder"
                @search="loadTargetOptions"
                @update:value="onTargetChange"
              />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup name="entityName">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('identity.field_security.label_entity') }}</XhFieldLabel>
            <XhFieldControl>
              <XCombobox
                class="fls-combobox"
                :options="entityOptions"
                :value="flsForm.entityName || null"
                :loading="entityLoading"
                :placeholder="t('identity.field_security.ph_entity')"
                @update:value="onEntityChange"
              />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup name="fieldName">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('identity.field_security.label_field') }}</XhFieldLabel>
            <XhFieldControl>
              <XCombobox
                class="fls-combobox"
                :options="fieldOptions"
                :value="flsForm.fieldName || null"
                :disabled="!currentEntity"
                :placeholder="t('identity.field_security.ph_field')"
                @update:value="onFieldChange"
              />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup name="maskStrategy">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('identity.field_security.label_mask_strategy') }}</XhFieldLabel>
            <XhFieldControl>
              <XSelect :value="flsForm.maskStrategy" :options="formMaskStrategyOptions" @update:value="onMaskStrategyChange" />
            </XhFieldControl>
            <XhFieldDescription v-if="currentField && !currentField.isText">
              {{ t('identity.field_security.hint_non_text') }}
            </XhFieldDescription>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup name="isEditable">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('identity.field_security.label_editable') }}</XhFieldLabel>
            <XhFieldControl>
              <XhSwitch v-model:checked="flsForm.isEditable" :disabled="!isMasked" />
            </XhFieldControl>
            <XhFieldDescription>
              {{ isMasked ? t('identity.field_security.hint_write_only') : t('identity.field_security.hint_plain_read_only') }}
            </XhFieldDescription>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <template v-if="flsForm.maskStrategy === FieldMaskStrategy.PartialMask">
          <XhFormFieldGroup name="maskKeepHead">
            <XhFieldRoot>
              <XhFieldLabel>{{ t('identity.field_security.label_keep_head') }}</XhFieldLabel>
              <XhFieldControl>
                <XNumberInput v-model:value="flsForm.maskKeepHead" :min="0" :max="MAX_MASK_KEEP" />
              </XhFieldControl>
              <XhFieldErrorText />
            </XhFieldRoot>
          </XhFormFieldGroup>
          <XhFormFieldGroup name="maskKeepTail">
            <XhFieldRoot>
              <XhFieldLabel>{{ t('identity.field_security.label_keep_tail') }}</XhFieldLabel>
              <XhFieldControl>
                <XNumberInput v-model:value="flsForm.maskKeepTail" :min="0" :max="MAX_MASK_KEEP" />
              </XhFieldControl>
              <XhFieldErrorText />
            </XhFieldRoot>
          </XhFormFieldGroup>
        </template>
        <XhFormFieldGroup v-if="flsForm.maskStrategy === FieldMaskStrategy.Redact" name="maskReplacement">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('identity.field_security.label_replacement') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="flsForm.maskReplacement" clearable :max-length="100" :placeholder="t('identity.field_security.ph_replacement')" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup v-if="!flsForm.basicId" name="status">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('identity.field_security.label_status') }}</XhFieldLabel>
            <XhFieldControl>
              <XSelect v-model:value="flsForm.status" :options="statusEnumOptions" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup name="remark" class="xh-span-2">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('identity.field_security.label_remark') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="flsForm.remark" clearable :placeholder="t('identity.field_security.ph_remark')" />
            </XhFieldControl>
            <XhFieldErrorText />
          </XhFieldRoot>
        </XhFormFieldGroup>
      </XhFormRoot>
    </XEditModal>
  </SchemaPage>
</template>

<style scoped>
/* 组合框默认按内容定宽，表单里与下拉框一样铺满字段 */
.fls-combobox {
  inline-size: 100%;
}

/* 列 render 的 h() 节点不带本页 scope，样式须经 :deep() 才能命中 */
:deep(.fls-invalid) {
  display: inline-flex;
  align-items: center;
  gap: var(--xh-space-1);
  color: var(--xh-fg-muted);
}
</style>
