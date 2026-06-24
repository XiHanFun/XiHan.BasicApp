<script setup lang="ts">
import type { SelectMixedOption } from 'naive-ui/es/select/src/interface'
import type {
  ApiId,
  ConstraintRuleDetailDto,
  ConstraintRuleItemDto,
  ConstraintRuleItemInputDto,
  ConstraintRuleListItemDto,
  PageResult,
} from '@/api'
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import {
  NButton,
  NDatePicker,
  NDescriptions,
  NDescriptionsItem,
  NDrawer,
  NDrawerContent,
  NEmpty,
  NForm,
  NFormItem,
  NInput,
  NInputNumber,
  NModal,
  NScrollbar,
  NSelect,
  NSpace,
  NSpin,
  NTag,
  useDialog,
  useMessage,
} from 'naive-ui'
import { computed, h, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import {
  constraintRuleApi,
  ConstraintTargetType,
  ConstraintType,
  createPageRequest,
  EnableStatus,
  permissionApi,
  querySortsFromSchema,
  roleApi,
  tenantMemberApi,
  ViolationAction,
} from '@/api'
import { SchemaPage } from '~/components'
import { CONSTRAINT_TYPE_OPTIONS, STATUS_OPTIONS, VIOLATION_ACTION_OPTIONS } from '~/constants'
import { useEnumOptions } from '~/hooks'
import { formatDate, getOptionLabel } from '~/utils'

defineOptions({ name: 'ApprovalConstraintPage' })

interface TargetOption {
  label: string
  value: ApiId
}

interface ConstraintItemFormModel {
  constraintGroup: number | null
  remark: string | null
  targetId: ApiId | null
  targetType: ConstraintTargetType
}

interface ConstraintRuleFormModel {
  basicId?: ApiId
  constraintType: ConstraintType
  description: string | null
  effectiveTime: number | null
  expirationTime: number | null
  items: ConstraintItemFormModel[]
  parameters: string | null
  priority: number | null
  remark: string | null
  ruleCode: string
  ruleName: string
  status: EnableStatus
  targetType: ConstraintTargetType
  violationAction: ViolationAction
}

const { t } = useI18n()
const message = useMessage()
const dialog = useDialog()

const statusOptions = useEnumOptions('EnableStatus', STATUS_OPTIONS)
const constraintTypeOptions = useEnumOptions('ConstraintType', CONSTRAINT_TYPE_OPTIONS)
const violationActionOptions = useEnumOptions('ViolationAction', VIOLATION_ACTION_OPTIONS)

const targetTypeOptions = computed(() => [
  { label: t('approval.constraint.target_role'), value: ConstraintTargetType.Role },
  { label: t('approval.constraint.target_permission'), value: ConstraintTargetType.Permission },
  { label: t('approval.constraint.target_user'), value: ConstraintTargetType.User },
])

const VIOLATION_TAG_TYPE: Record<ViolationAction, 'default' | 'error' | 'info' | 'warning'> = {
  [ViolationAction.Deny]: 'error',
  [ViolationAction.Warning]: 'warning',
  [ViolationAction.Log]: 'default',
  [ViolationAction.RequireApproval]: 'info',
}

/** 不同约束类型下规则项的填写提示 */
const ITEM_HINTS = computed<Partial<Record<ConstraintType, string>>>(() => ({
  [ConstraintType.SSD]: t('approval.constraint.hint_ssd'),
  [ConstraintType.DSD]: t('approval.constraint.hint_dsd'),
  [ConstraintType.MutualExclusion]: t('approval.constraint.hint_mutual_exclusion'),
  [ConstraintType.Cardinality]: t('approval.constraint.hint_cardinality'),
  [ConstraintType.Prerequisite]: t('approval.constraint.hint_prerequisite'),
}))

const schemaPageRef = ref<{ reload: () => Promise<void> } | null>(null)

function reload() {
  void schemaPageRef.value?.reload()
}

// ── 通用辅助 ────────────────────────────────────────────────────
function toStr(value: unknown): string | undefined {
  return (value as string | undefined)?.trim() || undefined
}

function normalizeNullable(value?: string | null) {
  const normalized = value?.trim()
  return normalized || null
}

function formatNullable(value: unknown) {
  return value === null || value === undefined || value === '' ? '-' : String(value)
}

function formatNullableDate(value?: string | null) {
  return value ? formatDate(value) : '-'
}

// ── 字段单一事实源：列 + 搜索 ───────────────────────────────────
const fields = computed<ListFieldSchema[]>(() => [
  { key: 'keyword', title: t('approval.constraint.keyword'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('approval.constraint.keyword_placeholder'), width: 240, order: 0 },
  { key: 'ruleCode', title: t('approval.constraint.rule_code'), dataType: 'string', sortable: true, minWidth: 160, order: 1 },
  { key: 'ruleName', title: t('approval.constraint.rule_name'), dataType: 'string', sortable: true, minWidth: 160, order: 2 },
  {
    key: 'constraintType',
    title: t('approval.constraint.constraint_type'),
    dataType: 'enum',
    searchable: true,
    searchMultiple: true,
    dictionaryCode: 'ConstraintType',
    options: constraintTypeOptions.value,
    searchPlaceholder: t('approval.constraint.constraint_type_placeholder'),
    minWidth: 130,
    order: 3,
    render: row => h(
      NTag,
      { size: 'small', round: true, bordered: false, type: 'info' },
      () => getOptionLabel(constraintTypeOptions.value, (row as unknown as ConstraintRuleListItemDto).constraintType),
    ),
  },
  {
    key: 'targetType',
    title: t('approval.constraint.target_type'),
    dataType: 'enum',
    options: targetTypeOptions.value,
    width: 96,
    order: 4,
    render: row => h('span', { style: 'font-size:13px;color:var(--n-text-color-3);' }, getOptionLabel(targetTypeOptions.value, (row as unknown as ConstraintRuleListItemDto).targetType)),
  },
  {
    key: 'violationAction',
    title: t('approval.constraint.violation_action'),
    dataType: 'enum',
    dictionaryCode: 'ViolationAction',
    options: violationActionOptions.value,
    width: 110,
    order: 5,
    render: (row) => {
      const r = row as unknown as ConstraintRuleListItemDto
      return h(
        NTag,
        { size: 'small', round: true, bordered: false, type: VIOLATION_TAG_TYPE[r.violationAction] ?? 'default' },
        () => getOptionLabel(violationActionOptions.value, r.violationAction),
      )
    },
  },
  { key: 'itemCount', title: t('approval.constraint.item_count'), dataType: 'number', width: 92, order: 6 },
  { key: 'priority', title: t('approval.constraint.priority'), dataType: 'number', sortable: true, width: 88, order: 7 },
  {
    key: 'status',
    title: t('approval.constraint.status'),
    dataType: 'enum',
    searchable: true,
    searchMultiple: true,
    sortable: true,
    dictionaryCode: 'EnableStatus',
    options: statusOptions.value,
    searchPlaceholder: t('approval.constraint.status_placeholder'),
    width: 82,
    order: 8,
    render: row => h(
      NTag,
      { size: 'small', round: true, bordered: false, type: (row as unknown as ConstraintRuleListItemDto).status === EnableStatus.Enabled ? 'success' : 'error' },
      () => (row as unknown as ConstraintRuleListItemDto).status === EnableStatus.Enabled ? t('approval.constraint.status_enabled') : t('approval.constraint.status_disabled'),
    ),
  },
  {
    key: 'isActive',
    title: t('approval.constraint.is_active'),
    dataType: 'boolean',
    width: 92,
    order: 9,
    render: row => h(
      NTag,
      { size: 'small', round: true, bordered: false, type: (row as unknown as ConstraintRuleListItemDto).isActive ? 'success' : 'default' },
      () => (row as unknown as ConstraintRuleListItemDto).isActive ? t('approval.constraint.active_yes') : t('approval.constraint.active_no'),
    ),
  },
  { key: 'description', title: t('approval.constraint.description'), dataType: 'string', minWidth: 200, order: 10 },
  {
    key: 'createdTime',
    title: t('approval.constraint.created_time'),
    dataType: 'datetime',
    sortable: true,
    minWidth: 170,
    order: 11,
    render: row => h('span', { style: 'font-size:13px;color:var(--n-text-color-3);' }, formatDate((row as unknown as ConstraintRuleListItemDto).createdTime)),
  },
])

// ── 资源适配器：归一化查询参数 → 后端 API ──────────────────────
const schema = computed<PageSchema>(() => ({
  pageCode: 'approval.constraint',
  exportPermission: 'saas:constraint-rule:export',
  pageName: t('approval.constraint.page_name'),
  statusPermission: 'saas:constraint-rule:status',
  rowKey: 'basicId',
  scrollX: 1600,
  fields: fields.value,
  resource: {
    page: (params) => {
      const f = params.filters
      return constraintRuleApi.page({
        ...createPageRequest({
          page: { pageIndex: params.page, pageSize: params.pageSize },
          // 排序 + 多选(constraintType/status) 等通用过滤统一走 conditions.filters In
          conditions: { sorts: querySortsFromSchema(params.sorts), filters: params.conditionFilters ?? [] },
        }),
        keyword: toStr(f.keyword) ?? null,
        // constraintType / status 改为多选，经 conditions.filters In 下发（不再走 DTO 顶层单值字段）
      }) as unknown as Promise<PageResult<Record<string, unknown>>>
    },
    updateStatus: (id, enabled) => constraintRuleApi.updateStatus({ basicId: id, status: enabled ? EnableStatus.Enabled : EnableStatus.Disabled, remark: enabled ? t('approval.constraint.remark_batch_enable') : t('approval.constraint.remark_batch_disable') }),
  },
  actions: [
    { key: 'create', title: t('approval.constraint.action_create'), scope: 'page', type: 'primary', icon: 'lucide:plus', permission: 'saas:constraint-rule:create' },
    { key: 'view', title: t('approval.constraint.action_view'), scope: 'row', permission: 'saas:constraint-rule:read' },
    { key: 'edit', title: t('approval.constraint.action_edit'), scope: 'row', permission: 'saas:constraint-rule:update' },
    { key: 'toggle', title: t('approval.constraint.action_toggle'), scope: 'row', permission: 'saas:constraint-rule:status' },
    { key: 'delete', title: t('approval.constraint.action_delete'), scope: 'row', type: 'error', permission: 'saas:constraint-rule:delete' },
  ],
}))

// ── 行/页面操作分发 ─────────────────────────────────────────────
function onAction(payload: SchemaActionPayload) {
  const row = payload.row as unknown as ConstraintRuleListItemDto | undefined
  switch (payload.key) {
    case 'create':
      handleAdd()
      break
    case 'view':
      if (row) {
        void handleView(row)
      }
      break
    case 'edit':
      if (row) {
        void handleEdit(row)
      }
      break
    case 'toggle':
      if (row) {
        confirmToggleStatus(row)
      }
      break
    case 'delete':
      if (row) {
        confirmDelete(row)
      }
      break
  }
}

// ── 目标候选（角色 / 权限 / 租户成员，远程搜索） ────────────────
const targetOptions = ref<Record<ConstraintTargetType, TargetOption[]>>({
  [ConstraintTargetType.Role]: [],
  [ConstraintTargetType.Permission]: [],
  [ConstraintTargetType.User]: [],
})
const targetLoading = ref<Record<ConstraintTargetType, boolean>>({
  [ConstraintTargetType.Role]: false,
  [ConstraintTargetType.Permission]: false,
  [ConstraintTargetType.User]: false,
})

function mergeOptions(current: TargetOption[], next: TargetOption[]) {
  const map = new Map<ApiId, TargetOption>()
  for (const option of current) {
    map.set(option.value, option)
  }
  for (const option of next) {
    map.set(option.value, option)
  }
  return [...map.values()]
}

async function loadTargetOptions(type: ConstraintTargetType, keyword = '') {
  targetLoading.value[type] = true
  try {
    let next: TargetOption[] = []
    if (type === ConstraintTargetType.Role) {
      const roles = await roleApi.enabledList({ limit: 50, keyword: normalizeNullable(keyword) })
      next = roles.map(role => ({ label: `${role.roleName} (${role.roleCode})`, value: role.basicId }))
    }
    else if (type === ConstraintTargetType.Permission) {
      const result = await permissionApi.page({
        ...createPageRequest({ page: { pageIndex: 1, pageSize: 50 } }),
        keyword: normalizeNullable(keyword),
        status: EnableStatus.Enabled,
      })
      next = result.items.map(permission => ({ label: `${permission.permissionName} (${permission.permissionCode})`, value: permission.basicId }))
    }
    else {
      const result = await tenantMemberApi.page({
        ...createPageRequest({ page: { pageIndex: 1, pageSize: 50 } }),
        keyword: normalizeNullable(keyword),
      })
      next = result.items.map(member => ({
        label: member.displayName ? `${member.displayName} (#${member.userId})` : t('approval.constraint.member_label', { id: member.userId }),
        value: member.userId,
      }))
    }
    targetOptions.value[type] = mergeOptions(targetOptions.value[type], next)
  }
  catch (e) {
    message.error((e as Error).message || t('approval.constraint.err_load_targets'))
  }
  finally {
    targetLoading.value[type] = false
  }
}

/** 编辑/详情回填时，把已有目标的名称种子化进候选，确保选中项有可读标签 */
function seedTargetOption(item: ConstraintRuleItemDto) {
  const label = item.targetName
    ? (item.targetCode ? `${item.targetName} (${item.targetCode})` : item.targetName)
    : `#${item.targetId}`
  targetOptions.value[item.targetType] = mergeOptions(targetOptions.value[item.targetType], [{ label, value: item.targetId }])
}

// ── 详情抽屉 ────────────────────────────────────────────────────
const detailVisible = ref(false)
const detailLoading = ref(false)
const currentDetail = ref<ConstraintRuleDetailDto | null>(null)

const parametersPretty = computed(() => {
  const parameters = currentDetail.value?.parameters
  if (!parameters) {
    return null
  }
  try {
    return JSON.stringify(JSON.parse(parameters), null, 2)
  }
  catch {
    return parameters
  }
})

function formatItemGroup(item: ConstraintRuleItemDto) {
  if (currentDetail.value?.constraintType !== ConstraintType.Prerequisite) {
    return String(item.constraintGroup)
  }
  if (item.constraintGroup === 0) {
    return t('approval.constraint.group_prerequisite')
  }
  if (item.constraintGroup === 1) {
    return t('approval.constraint.group_target')
  }
  return String(item.constraintGroup)
}

async function handleView(row: ConstraintRuleListItemDto) {
  detailVisible.value = true
  detailLoading.value = true
  currentDetail.value = null
  try {
    currentDetail.value = await constraintRuleApi.detail(row.basicId)
    if (!currentDetail.value) {
      message.warning(t('approval.constraint.warn_detail_not_found'))
    }
  }
  catch (e) {
    message.error((e as Error).message || t('approval.constraint.err_load_detail'))
  }
  finally {
    detailLoading.value = false
  }
}

// ── 新增 / 编辑表单 ─────────────────────────────────────────────
const modalVisible = ref(false)
const submitLoading = ref(false)
const editingStatus = ref<EnableStatus | null>(null)
const ruleForm = ref<ConstraintRuleFormModel>(createDefaultForm())

const modalTitle = computed(() => (ruleForm.value.basicId ? t('approval.constraint.modal_title_edit') : t('approval.constraint.modal_title_add')))
const isPrerequisite = computed(() => ruleForm.value.constraintType === ConstraintType.Prerequisite)
const itemHint = computed(() => ITEM_HINTS.value[ruleForm.value.constraintType] ?? t('approval.constraint.hint_default'))

function createItem(targetType: ConstraintTargetType): ConstraintItemFormModel {
  return { constraintGroup: 0, remark: null, targetId: null, targetType }
}

function createDefaultForm(): ConstraintRuleFormModel {
  return {
    constraintType: ConstraintType.SSD,
    description: null,
    effectiveTime: null,
    expirationTime: null,
    items: [createItem(ConstraintTargetType.Role)],
    parameters: null,
    priority: 0,
    remark: null,
    ruleCode: '',
    ruleName: '',
    status: EnableStatus.Enabled,
    targetType: ConstraintTargetType.Role,
    violationAction: ViolationAction.Deny,
  }
}

function handleAdd() {
  editingStatus.value = null
  ruleForm.value = createDefaultForm()
  modalVisible.value = true
  void loadTargetOptions(ruleForm.value.targetType)
}

async function handleEdit(row: ConstraintRuleListItemDto) {
  try {
    const detail = await constraintRuleApi.detail(row.basicId)
    if (!detail) {
      message.warning(t('approval.constraint.warn_detail_not_found'))
      return
    }
    for (const item of detail.items) {
      seedTargetOption(item)
    }
    editingStatus.value = detail.status
    ruleForm.value = {
      basicId: detail.basicId,
      constraintType: detail.constraintType,
      description: detail.description ?? null,
      effectiveTime: detail.effectiveTime ? new Date(detail.effectiveTime).getTime() : null,
      expirationTime: detail.expirationTime ? new Date(detail.expirationTime).getTime() : null,
      items: detail.items.map(item => ({
        constraintGroup: item.constraintGroup,
        remark: item.remark ?? null,
        targetId: item.targetId,
        targetType: item.targetType,
      })),
      parameters: detail.parameters ?? null,
      priority: detail.priority,
      remark: detail.remark ?? null,
      ruleCode: detail.ruleCode,
      ruleName: detail.ruleName,
      status: detail.status,
      targetType: detail.targetType,
      violationAction: detail.violationAction,
    }
    modalVisible.value = true
    void loadTargetOptions(detail.targetType)
  }
  catch (e) {
    message.error((e as Error).message || t('approval.constraint.err_load_detail'))
  }
}

// ── 规则项动态行编辑 ────────────────────────────────────────────
function addItem() {
  ruleForm.value.items.push(createItem(ruleForm.value.targetType))
}

function removeItem(index: number) {
  ruleForm.value.items.splice(index, 1)
}

/** 非先决条件约束：规则项目标类型必须与规则目标类型一致（后端强校验，前端直接同步） */
function syncItemTargetTypes() {
  if (isPrerequisite.value) {
    return
  }
  for (const item of ruleForm.value.items) {
    if (item.targetType !== ruleForm.value.targetType) {
      item.targetType = ruleForm.value.targetType
      item.targetId = null
    }
  }
}

function onRuleTargetTypeChange() {
  syncItemTargetTypes()
  void loadTargetOptions(ruleForm.value.targetType)
}

function onConstraintTypeChange() {
  syncItemTargetTypes()
}

function onItemTargetTypeChange(item: ConstraintItemFormModel) {
  item.targetId = null
  void loadTargetOptions(item.targetType)
}

// ── 提交 ────────────────────────────────────────────────────────
function validateForm(): boolean {
  const form = ruleForm.value
  if (!form.basicId && !form.ruleCode.trim()) {
    message.warning(t('approval.constraint.warn_input_rule_code'))
    return false
  }
  if (!form.ruleName.trim()) {
    message.warning(t('approval.constraint.warn_input_rule_name'))
    return false
  }
  if (form.parameters?.trim()) {
    try {
      JSON.parse(form.parameters)
    }
    catch {
      message.warning(t('approval.constraint.warn_invalid_json'))
      return false
    }
  }
  if (form.effectiveTime != null && form.expirationTime != null && form.effectiveTime >= form.expirationTime) {
    message.warning(t('approval.constraint.warn_time_order'))
    return false
  }
  if (form.items.length === 0) {
    message.warning(t('approval.constraint.warn_at_least_one_item'))
    return false
  }
  for (const [index, item] of form.items.entries()) {
    if (!item.targetId) {
      message.warning(t('approval.constraint.warn_item_no_target', { index: index + 1 }))
      return false
    }
    if (item.constraintGroup == null || item.constraintGroup < 0) {
      message.warning(t('approval.constraint.warn_item_group_negative', { index: index + 1 }))
      return false
    }
  }
  const targetKeys = new Set(form.items.map(item => `${item.targetType}:${item.targetId}`))
  if (targetKeys.size !== form.items.length) {
    message.warning(t('approval.constraint.warn_target_duplicate'))
    return false
  }
  const needsPair = [ConstraintType.SSD, ConstraintType.DSD, ConstraintType.MutualExclusion]
  if (needsPair.includes(form.constraintType) && form.items.length < 2) {
    message.warning(t('approval.constraint.warn_need_pair'))
    return false
  }
  if (form.constraintType === ConstraintType.Prerequisite) {
    const groups = new Set(form.items.map(item => item.constraintGroup))
    if (!groups.has(0) || !groups.has(1)) {
      message.warning(t('approval.constraint.warn_prerequisite_groups'))
      return false
    }
  }
  return true
}

async function handleSubmit() {
  if (!validateForm()) {
    return
  }
  const form = ruleForm.value
  submitLoading.value = true
  try {
    const items: ConstraintRuleItemInputDto[] = form.items.map(item => ({
      constraintGroup: item.constraintGroup ?? 0,
      remark: normalizeNullable(item.remark),
      targetId: item.targetId!,
      targetType: item.targetType,
    }))
    const shared = {
      constraintType: form.constraintType,
      description: normalizeNullable(form.description),
      effectiveTime: form.effectiveTime != null ? new Date(form.effectiveTime).toISOString() : null,
      expirationTime: form.expirationTime != null ? new Date(form.expirationTime).toISOString() : null,
      items,
      parameters: normalizeNullable(form.parameters),
      priority: form.priority ?? 0,
      remark: normalizeNullable(form.remark),
      ruleName: form.ruleName.trim(),
      targetType: form.targetType,
      violationAction: form.violationAction,
    }
    if (form.basicId) {
      await constraintRuleApi.update({ basicId: form.basicId, ...shared })
      if (editingStatus.value !== form.status) {
        await constraintRuleApi.updateStatus({
          basicId: form.basicId,
          remark: shared.remark,
          status: form.status,
        })
      }
    }
    else {
      await constraintRuleApi.create({
        ...shared,
        ruleCode: form.ruleCode.trim(),
        status: form.status,
      })
    }
    message.success(t('approval.constraint.msg_save_success'))
    modalVisible.value = false
    reload()
  }
  catch (e) {
    message.error((e as Error).message || t('approval.constraint.msg_save_failed'))
  }
  finally {
    submitLoading.value = false
  }
}

// ── 启停 / 删除 ─────────────────────────────────────────────────
function confirmToggleStatus(row: ConstraintRuleListItemDto) {
  const enable = row.status !== EnableStatus.Enabled
  dialog.warning({
    title: enable ? t('approval.constraint.toggle_enable_title') : t('approval.constraint.toggle_disable_title'),
    content: enable ? t('approval.constraint.toggle_enable_content', { name: row.ruleName }) : t('approval.constraint.toggle_disable_content', { name: row.ruleName }),
    positiveText: enable ? t('approval.constraint.toggle_positive_enable') : t('approval.constraint.toggle_positive_disable'),
    negativeText: t('approval.constraint.cancel'),
    onPositiveClick: async () => {
      try {
        await constraintRuleApi.updateStatus({
          basicId: row.basicId,
          remark: enable ? t('approval.constraint.remark_fe_enable') : t('approval.constraint.remark_fe_disable'),
          status: enable ? EnableStatus.Enabled : EnableStatus.Disabled,
        })
        message.success(t('approval.constraint.msg_status_updated'))
        reload()
      }
      catch (e) {
        message.error((e as Error).message || t('approval.constraint.msg_status_update_failed'))
      }
    },
  })
}

function confirmDelete(row: ConstraintRuleListItemDto) {
  dialog.error({
    title: t('approval.constraint.delete_title'),
    content: t('approval.constraint.delete_content', { name: row.ruleName, count: row.itemCount }),
    positiveText: t('approval.constraint.btn_delete'),
    negativeText: t('approval.constraint.cancel'),
    onPositiveClick: async () => {
      try {
        await constraintRuleApi.delete(row.basicId)
        message.success(t('approval.constraint.msg_delete_success'))
        reload()
      }
      catch (e) {
        message.error((e as Error).message || t('approval.constraint.msg_delete_failed'))
      }
    },
  })
}
</script>

<template>
  <SchemaPage
    ref="schemaPageRef"
    :schema="schema"
    @action="onAction"
  >
    <!-- 详情抽屉：规则 + 明细项列表 -->
    <NDrawer v-model:show="detailVisible" :width="880">
      <NDrawerContent closable :title="t('approval.constraint.detail_title')">
        <NSpin :show="detailLoading">
          <NEmpty v-if="!detailLoading && !currentDetail" class="rule-detail-empty" :description="t('approval.constraint.empty_detail')" />
          <NScrollbar v-else-if="currentDetail" style="max-height: calc(100vh - 120px)">
            <NDescriptions :column="2" bordered size="small">
              <NDescriptionsItem :label="t('approval.constraint.rule_code')">
                {{ currentDetail.ruleCode }}
              </NDescriptionsItem>
              <NDescriptionsItem :label="t('approval.constraint.rule_name')">
                {{ currentDetail.ruleName }}
              </NDescriptionsItem>
              <NDescriptionsItem :label="t('approval.constraint.constraint_type')">
                {{ getOptionLabel(constraintTypeOptions, currentDetail.constraintType) }}
              </NDescriptionsItem>
              <NDescriptionsItem :label="t('approval.constraint.target_type')">
                {{ getOptionLabel(targetTypeOptions, currentDetail.targetType) }}
              </NDescriptionsItem>
              <NDescriptionsItem :label="t('approval.constraint.violation_action')">
                {{ getOptionLabel(violationActionOptions, currentDetail.violationAction) }}
              </NDescriptionsItem>
              <NDescriptionsItem :label="t('approval.constraint.priority')">
                {{ currentDetail.priority }}
              </NDescriptionsItem>
              <NDescriptionsItem :label="t('approval.constraint.status')">
                {{ getOptionLabel(statusOptions, currentDetail.status) }}
              </NDescriptionsItem>
              <NDescriptionsItem :label="t('approval.constraint.is_active')">
                {{ currentDetail.isActive ? t('approval.constraint.active_yes') : t('approval.constraint.active_no') }}
              </NDescriptionsItem>
              <NDescriptionsItem :label="t('approval.constraint.is_global')">
                {{ currentDetail.isGlobal ? t('approval.constraint.yes') : t('approval.constraint.no') }}
              </NDescriptionsItem>
              <NDescriptionsItem :label="t('approval.constraint.effective_range')">
                {{ formatNullableDate(currentDetail.effectiveTime) }} {{ t('approval.constraint.effective_range_to') }} {{ formatNullableDate(currentDetail.expirationTime) }}
              </NDescriptionsItem>
              <NDescriptionsItem :label="t('approval.constraint.description')">
                {{ formatNullable(currentDetail.description) }}
              </NDescriptionsItem>
              <NDescriptionsItem :label="t('approval.constraint.label_remark')">
                {{ formatNullable(currentDetail.remark) }}
              </NDescriptionsItem>
              <NDescriptionsItem :label="t('approval.constraint.created')">
                {{ formatNullable(currentDetail.createdBy) }} · {{ formatNullableDate(currentDetail.createdTime) }}
              </NDescriptionsItem>
              <NDescriptionsItem :label="t('approval.constraint.last_modified')">
                {{ formatNullable(currentDetail.modifiedBy) }} · {{ formatNullableDate(currentDetail.modifiedTime) }}
              </NDescriptionsItem>
            </NDescriptions>

            <template v-if="parametersPretty">
              <h4 class="rule-detail-subtitle">
                {{ t('approval.constraint.parameters') }}
              </h4>
              <pre class="rule-params">{{ parametersPretty }}</pre>
            </template>

            <h4 class="rule-detail-subtitle">
              {{ t('approval.constraint.items_count', { count: currentDetail.items.length }) }}
            </h4>
            <table v-if="currentDetail.items.length" class="rule-detail-table">
              <thead>
                <tr>
                  <th>{{ t('approval.constraint.col_group') }}</th>
                  <th>{{ t('approval.constraint.col_target_type') }}</th>
                  <th>{{ t('approval.constraint.col_target_name') }}</th>
                  <th>{{ t('approval.constraint.col_target_code') }}</th>
                  <th>{{ t('approval.constraint.col_remark') }}</th>
                  <th>{{ t('approval.constraint.col_created_time') }}</th>
                </tr>
              </thead>
              <tbody>
                <tr v-for="item in currentDetail.items" :key="String(item.basicId)">
                  <td>{{ formatItemGroup(item) }}</td>
                  <td>{{ getOptionLabel(targetTypeOptions, item.targetType) }}</td>
                  <td>{{ item.targetName || `#${item.targetId}` }}</td>
                  <td>{{ formatNullable(item.targetCode) }}</td>
                  <td>{{ formatNullable(item.remark) }}</td>
                  <td>{{ formatNullableDate(item.createdTime) }}</td>
                </tr>
              </tbody>
            </table>
            <NEmpty v-else :description="t('approval.constraint.empty_items')" style="padding: 24px 0" />
          </NScrollbar>
        </NSpin>
      </NDrawerContent>
    </NDrawer>

    <!-- 新增 / 编辑弹窗：基础字段 + 规则项动态行 -->
    <NModal
      v-model:show="modalVisible"
      :auto-focus="false"
      :bordered="false"
      :title="modalTitle"
      preset="card"
      style="width: 860px; max-width: 94vw"
    >
      <NForm :model="ruleForm" class="xh-edit-form-grid" label-placement="top">
        <NFormItem :label="t('approval.constraint.label_rule_code')" path="ruleCode">
          <NInput
            v-model:value="ruleForm.ruleCode"
            clearable
            :disabled="Boolean(ruleForm.basicId)"
            :placeholder="t('approval.constraint.placeholder_rule_code')"
          />
        </NFormItem>
        <NFormItem :label="t('approval.constraint.label_rule_name')" path="ruleName">
          <NInput v-model:value="ruleForm.ruleName" clearable :placeholder="t('approval.constraint.placeholder_rule_name')" />
        </NFormItem>
        <NFormItem :label="t('approval.constraint.label_constraint_type')" path="constraintType">
          <NSelect
            v-model:value="ruleForm.constraintType"
            :options="constraintTypeOptions as unknown as SelectMixedOption[]"
            @update:value="onConstraintTypeChange"
          />
        </NFormItem>
        <NFormItem :label="t('approval.constraint.label_target_type')" path="targetType">
          <NSelect
            v-model:value="ruleForm.targetType"
            :options="targetTypeOptions"
            @update:value="onRuleTargetTypeChange"
          />
        </NFormItem>
        <NFormItem :label="t('approval.constraint.label_violation_action')" path="violationAction">
          <NSelect v-model:value="ruleForm.violationAction" :options="violationActionOptions as unknown as SelectMixedOption[]" />
        </NFormItem>
        <NFormItem :label="t('approval.constraint.label_priority')" path="priority">
          <NInputNumber v-model:value="ruleForm.priority" :min="0" style="width: 100%" />
        </NFormItem>
        <NFormItem :label="t('approval.constraint.label_status')" path="status">
          <NSelect v-model:value="ruleForm.status" :options="statusOptions as unknown as SelectMixedOption[]" />
        </NFormItem>
        <NFormItem :label="t('approval.constraint.label_remark')" path="remark">
          <NInput v-model:value="ruleForm.remark" clearable :placeholder="t('approval.constraint.placeholder_remark')" />
        </NFormItem>
        <NFormItem :label="t('approval.constraint.label_effective_time')" path="effectiveTime">
          <NDatePicker v-model:value="ruleForm.effectiveTime" clearable style="width: 100%" type="datetime" />
        </NFormItem>
        <NFormItem :label="t('approval.constraint.label_expiration_time')" path="expirationTime">
          <NDatePicker v-model:value="ruleForm.expirationTime" clearable style="width: 100%" type="datetime" />
        </NFormItem>
        <NFormItem class="xh-form-full-row" :label="t('approval.constraint.label_parameters')" path="parameters">
          <NInput
            v-model:value="ruleForm.parameters"
            clearable
            :placeholder="t('approval.constraint.placeholder_parameters')"
            :rows="2"
            type="textarea"
          />
        </NFormItem>
        <NFormItem class="xh-form-full-row" :label="t('approval.constraint.label_description')" path="description">
          <NInput
            v-model:value="ruleForm.description"
            clearable
            :placeholder="t('approval.constraint.placeholder_description')"
            :rows="2"
            type="textarea"
          />
        </NFormItem>
      </NForm>

      <div class="rule-items">
        <div class="rule-items__head">
          <span class="rule-items__title">{{ t('approval.constraint.items_title', { count: ruleForm.items.length }) }}</span>
          <span class="rule-items__hint">{{ itemHint }}</span>
          <NButton dashed size="small" @click="addItem">
            {{ t('approval.constraint.add_item') }}
          </NButton>
        </div>
        <NEmpty v-if="ruleForm.items.length === 0" :description="t('approval.constraint.empty_no_items')" style="padding: 20px 0" />
        <div v-for="(item, index) in ruleForm.items" :key="index" class="rule-item-row">
          <NInputNumber
            v-model:value="item.constraintGroup"
            :min="0"
            :placeholder="t('approval.constraint.placeholder_group')"
            size="small"
            style="width: 100px"
          />
          <NSelect
            v-model:value="item.targetType"
            :disabled="!isPrerequisite"
            :options="targetTypeOptions"
            size="small"
            style="width: 110px"
            @update:value="() => onItemTargetTypeChange(item)"
          />
          <NSelect
            v-model:value="item.targetId"
            clearable
            filterable
            :loading="targetLoading[item.targetType]"
            :options="targetOptions[item.targetType]"
            :placeholder="t('approval.constraint.placeholder_target')"
            remote
            size="small"
            style="flex: 1; min-width: 0"
            @focus="() => loadTargetOptions(item.targetType)"
            @search="(kw: string) => loadTargetOptions(item.targetType, kw)"
          />
          <NInput
            v-model:value="item.remark"
            clearable
            :placeholder="t('approval.constraint.placeholder_item_remark')"
            size="small"
            style="width: 160px"
          />
          <NButton quaternary size="small" type="error" @click="removeItem(index)">
            {{ t('approval.constraint.item_delete') }}
          </NButton>
        </div>
      </div>

      <template #footer>
        <NSpace justify="end">
          <NButton @click="modalVisible = false">
            {{ t('approval.constraint.cancel') }}
          </NButton>
          <NButton :loading="submitLoading" type="primary" @click="handleSubmit">
            {{ t('approval.constraint.save') }}
          </NButton>
        </NSpace>
      </template>
    </NModal>
  </SchemaPage>
</template>

<style scoped>
/* 详情抽屉 */
.rule-detail-empty {
  padding: 48px 0;
}

.rule-detail-subtitle {
  margin: 18px 0 8px;
  font-size: 13px;
  font-weight: 600;
}

.rule-params {
  margin: 0;
  padding: 10px 12px;
  border: 1px solid var(--n-border-color);
  border-radius: 8px;
  background: var(--n-merged-th-color, rgb(0 0 0 / 0.02));
  font-size: 12px;
  line-height: 1.6;
  white-space: pre-wrap;
  word-break: break-all;
}

.rule-detail-table {
  width: 100%;
  border-collapse: collapse;
  font-size: 13px;
}

.rule-detail-table th,
.rule-detail-table td {
  padding: 9px 10px;
  border: 1px solid var(--n-border-color);
  text-align: left;
  vertical-align: top;
}

.rule-detail-table th {
  background: var(--n-merged-th-color);
  font-weight: 500;
}

/* 规则项动态行编辑 */
.rule-items {
  margin-top: 4px;
  padding: 12px;
  border: 1px solid var(--n-border-color);
  border-radius: 8px;
}

.rule-items__head {
  display: flex;
  align-items: center;
  gap: 10px;
  margin-bottom: 10px;
}

.rule-items__title {
  font-size: 13px;
  font-weight: 600;
}

.rule-items__hint {
  flex: 1;
  font-size: 12px;
  opacity: 0.6;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.rule-item-row {
  display: flex;
  align-items: center;
  gap: 8px;
}

.rule-item-row + .rule-item-row {
  margin-top: 8px;
}
</style>
