<script setup lang="ts">
import type { ConditionGroupModel } from './access-policy'
import type {
  AccessPolicyAttributeDto,
  AccessPolicyConditionDto,
  AccessPolicyDetailDto,
  AccessPolicyListItemDto,
  AccessPolicyTemplateDto,
  ApiId,
  PageResult,
} from '@/api'
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import {
  XhAlertContent,
  XhAlertDescription,
  XhAlertIndicator,
  XhAlertRoot,
  XhButton,
  XhButtonLabel,
  XhDescriptionsItem,
  XhDescriptionsLabel,
  XhDescriptionsRoot,
  XhDescriptionsValue,
  XhDrawerBody,
  XhDrawerCloseTrigger,
  XhDrawerContent,
  XhDrawerHeader,
  XhDrawerRoot,
  XhDrawerTitle,
  XhFieldControl,
  XhFieldLabel,
  XhFieldRoot,
  XhFormFieldGroup,
  XhFormRoot,
  XhTagLabel,
  XhTagRoot,
} from '@xihan-ui/vue'
import { computed, h, ref, useId } from 'vue'
import { useI18n } from 'vue-i18n'
import { useRouter } from 'vue-router'
import { AccessPolicyAction, accessPolicyApi, createPageRequest, EnableStatus, querySortsFromSchema } from '@/api'
import { ACCESS_POLICY_ACTION_OPTIONS, ACCESS_POLICY_PHASE_OPTIONS, CONDITION_OPERATOR_OPTIONS, STATUS_OPTIONS } from '@/constants'
import { Icon, SchemaPage, XDatePicker, XEditModal, XInput, XNumberInput, XSelect } from '~/components'
import { dialog, toast } from '~/composables'
import { useEnumOptions, usePermission } from '~/hooks'
import { useUserStore } from '~/stores'
import { formatDate, getOptionLabel } from '~/utils'
import { actionTone, findIncomplete, toConditions, toGroups } from './access-policy'
import AccessPolicyConditionEditor from './components/AccessPolicyConditionEditor.vue'
import AccessPolicyTrialDrawer from './components/AccessPolicyTrialDrawer.vue'
import LoginBanDrawer from './components/LoginBanDrawer.vue'

defineOptions({ name: 'SettingAccessPolicyPage' })

interface PolicyFormModel {
  basicId?: ApiId
  policyCode: string
  policyName: string
  description: string | null
  priority: number | null
  status: EnableStatus
  effectiveTime: number | null
  expirationTime: number | null
  templateCode: string | null
  remark: string | null
}

const { t } = useI18n()
const router = useRouter()
const userStore = useUserStore()
const { hasPermission } = usePermission()
const editFormId = useId()

const actionOptions = useEnumOptions('AccessPolicyAction', ACCESS_POLICY_ACTION_OPTIONS)
const phaseOptions = useEnumOptions('AccessPolicyPhase', ACCESS_POLICY_PHASE_OPTIONS)
const statusOptions = useEnumOptions('EnableStatus', STATUS_OPTIONS)
const operatorOptions = useEnumOptions('ConditionOperator', CONDITION_OPERATOR_OPTIONS)

const isPlatform = computed(() => userStore.userInfo?.isPlatform ?? false)
const schemaPageRef = ref<{ reload: () => Promise<void> } | null>(null)
const catalog = ref<AccessPolicyAttributeDto[]>([])

function reload() {
  void schemaPageRef.value?.reload()
}

function toStr(value: unknown) {
  return (value as string | undefined)?.trim() || undefined
}

function normalizeNullable(value?: string | null) {
  return value?.trim() || null
}

async function ensureCatalog() {
  if (catalog.value.length) {
    return true
  }
  try {
    catalog.value = await accessPolicyApi.attributes()
    return true
  }
  catch (error) {
    toast.danger((error as Error).message || t('setting.access_policy.load_attributes_failed'))
    return false
  }
}

function actionTag(action: AccessPolicyAction) {
  return h(XhTagRoot, { variant: 'subtle', tone: actionTone(action) }, () => h(XhTagLabel, () => getOptionLabel(actionOptions.value, action)))
}

// ── 列表 ────────────────────────────────────────────────────────
const fields = computed<ListFieldSchema[]>(() => [
  { key: 'keyword', title: t('setting.access_policy.keyword'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('setting.access_policy.keyword_placeholder'), order: 0 },
  { key: 'policyCode', title: t('setting.access_policy.policy_code'), dataType: 'string', sortable: true, minWidth: 180, order: 1 },
  { key: 'policyName', title: t('setting.access_policy.policy_name'), dataType: 'string', sortable: true, minWidth: 180, order: 2 },
  {
    key: 'action',
    title: t('setting.access_policy.action'),
    dataType: 'enum',
    searchable: true,
    searchMultiple: true,
    sortable: true,
    options: actionOptions.value,
    searchPlaceholder: t('setting.access_policy.action_placeholder'),
    width: 100,
    order: 3,
    render: row => actionTag((row as unknown as AccessPolicyListItemDto).action),
  },
  {
    key: 'phase',
    title: t('setting.access_policy.phase'),
    dataType: 'enum',
    options: phaseOptions.value,
    width: 110,
    order: 4,
    render: row => getOptionLabel(phaseOptions.value, (row as unknown as AccessPolicyListItemDto).phase),
  },
  { key: 'priority', title: t('setting.access_policy.priority'), dataType: 'number', sortable: true, width: 88, order: 5 },
  {
    key: 'status',
    title: t('setting.access_policy.status'),
    dataType: 'enum',
    searchable: true,
    searchMultiple: true,
    sortable: true,
    options: statusOptions.value,
    searchPlaceholder: t('setting.access_policy.status_placeholder'),
    width: 88,
    order: 6,
    render: (row) => {
      const enabled = (row as unknown as AccessPolicyListItemDto).status === EnableStatus.Enabled
      return h(XhTagRoot, { variant: 'subtle', tone: enabled ? 'success' : 'neutral' }, () => h(XhTagLabel, () => getOptionLabel(statusOptions.value, (row as unknown as AccessPolicyListItemDto).status)))
    },
  },
  {
    key: 'isActive',
    title: t('setting.access_policy.is_active'),
    dataType: 'boolean',
    width: 92,
    order: 7,
    render: (row) => {
      const active = (row as unknown as AccessPolicyListItemDto).isActive
      return h(XhTagRoot, { variant: 'subtle', tone: active ? 'success' : 'neutral' }, () => h(XhTagLabel, () => active ? t('setting.access_policy.active_yes') : t('setting.access_policy.active_no')))
    },
  },
  { key: 'conditionCount', title: t('setting.access_policy.condition_count'), dataType: 'number', width: 88, order: 8 },
  { key: 'recentHitCount', title: t('setting.access_policy.recent_hits'), dataType: 'number', width: 110, order: 9 },
  { key: 'effectiveTime', title: t('setting.access_policy.effective_time'), dataType: 'datetime', minWidth: 170, order: 10 },
  { key: 'expirationTime', title: t('setting.access_policy.expiration_time'), dataType: 'datetime', minWidth: 170, order: 11 },
  { key: 'description', title: t('setting.access_policy.description'), dataType: 'string', minWidth: 220, order: 12 },
  { key: 'modifiedTime', title: t('setting.access_policy.modified_time'), dataType: 'datetime', sortable: true, minWidth: 170, order: 13 },
])

const schema = computed<PageSchema>(() => ({
  pageCode: 'setting.access-policy',
  pageName: t('setting.access_policy.page_name'),
  statusPermission: 'setting.access-policy.status',
  rowKey: 'basicId',
  fields: fields.value,
  resource: {
    page: (params) => {
      const f = params.filters
      return accessPolicyApi.page({
        ...createPageRequest({
          page: { pageIndex: params.page, pageSize: params.pageSize },
          conditions: { sorts: querySortsFromSchema(params.sorts), filters: params.conditionFilters ?? [] },
        }),
        keyword: toStr(f.keyword) ?? null,
      }) as unknown as Promise<PageResult<Record<string, unknown>>>
    },
    updateStatus: (id, enabled) => accessPolicyApi.updateStatus({ basicId: id, status: enabled ? EnableStatus.Enabled : EnableStatus.Disabled }),
  },
  actions: [
    { key: 'create', title: t('setting.access_policy.action_create'), scope: 'page', tone: 'brand', icon: 'lucide:plus', permission: 'setting.access-policy.create' },
    { key: 'from-template', title: t('setting.access_policy.action_from_template'), scope: 'page', icon: 'lucide:layout-template', permission: 'setting.access-policy.create' },
    ...(isPlatform.value ? [{ key: 'bans', title: t('setting.access_policy.action_bans'), scope: 'page' as const, icon: 'lucide:ban' }] : []),
    { key: 'view', title: t('setting.access_policy.action_view'), scope: 'row', icon: 'lucide:eye' },
    { key: 'edit', title: t('setting.access_policy.action_edit'), scope: 'row', icon: 'lucide:pencil', permission: 'setting.access-policy.update' },
    { key: 'trial', title: t('setting.access_policy.action_trial'), scope: 'row', icon: 'lucide:flask-conical', permission: 'setting.access-policy.trial' },
    {
      key: 'enable-blocking',
      title: t('setting.access_policy.action_enable_blocking'),
      scope: 'row',
      tone: 'warning',
      icon: 'lucide:shield-x',
      permission: 'setting.access-policy.update',
      visible: row => (row as unknown as AccessPolicyListItemDto).action !== AccessPolicyAction.Deny,
    },
    {
      key: 'set-allow',
      title: t('setting.access_policy.action_set_allow'),
      scope: 'row',
      icon: 'lucide:shield-check',
      permission: 'setting.access-policy.update',
      visible: row => (row as unknown as AccessPolicyListItemDto).action !== AccessPolicyAction.Allow,
    },
    {
      key: 'set-log-only',
      title: t('setting.access_policy.action_set_log_only'),
      scope: 'row',
      icon: 'lucide:notebook-pen',
      permission: 'setting.access-policy.update',
      visible: row => (row as unknown as AccessPolicyListItemDto).action !== AccessPolicyAction.LogOnly,
    },
    { key: 'toggle', title: t('setting.access_policy.action_toggle'), scope: 'row', icon: 'lucide:power', permission: 'setting.access-policy.status' },
    { key: 'hits', title: t('setting.access_policy.action_hits'), scope: 'row', icon: 'lucide:list-checks', permission: 'setting.access-policy.hits' },
    { key: 'delete', title: t('setting.access_policy.action_delete'), scope: 'row', tone: 'danger', icon: 'lucide:trash-2', permission: 'setting.access-policy.delete' },
  ],
}))

function onAction(payload: SchemaActionPayload) {
  const row = payload.row as unknown as AccessPolicyListItemDto | undefined
  switch (payload.key) {
    case 'create':
      void openCreate(null)
      break
    case 'from-template':
      void openTemplates()
      break
    case 'bans':
      openBans()
      break
    case 'view':
      if (row) {
        void openDetail(row)
      }
      break
    case 'edit':
      if (row) {
        void openEdit(row)
      }
      break
    case 'trial':
      if (row) {
        openTrial({ title: row.policyName, policyId: row.basicId, conditions: null, action: row.action })
      }
      break
    case 'enable-blocking':
      if (row) {
        confirmAction(row, AccessPolicyAction.Deny)
      }
      break
    case 'set-allow':
      if (row) {
        confirmAction(row, AccessPolicyAction.Allow)
      }
      break
    case 'set-log-only':
      if (row) {
        confirmAction(row, AccessPolicyAction.LogOnly)
      }
      break
    case 'toggle':
      if (row) {
        confirmToggle(row)
      }
      break
    case 'hits':
      if (row) {
        void router.push({ path: '/log/access-policy', query: { policyCode: row.policyCode } })
      }
      break
    case 'delete':
      if (row) {
        confirmDelete(row)
      }
      break
  }
}

// ── 详情 ────────────────────────────────────────────────────────
const detailVisible = ref(false)
const detail = ref<AccessPolicyDetailDto | null>(null)

async function openDetail(row: AccessPolicyListItemDto) {
  try {
    detail.value = await accessPolicyApi.detail(row.basicId)
    if (!detail.value) {
      toast.warning(t('setting.access_policy.not_found'))
      return
    }
    detailVisible.value = true
  }
  catch (error) {
    toast.danger((error as Error).message || t('setting.access_policy.load_detail_failed'))
  }
}

// ── 模板 ────────────────────────────────────────────────────────
const templatesVisible = ref(false)
const templates = ref<AccessPolicyTemplateDto[]>([])

async function openTemplates() {
  try {
    if (!templates.value.length) {
      templates.value = await accessPolicyApi.templates()
    }
    templatesVisible.value = true
  }
  catch (error) {
    toast.danger((error as Error).message || t('setting.access_policy.load_templates_failed'))
  }
}

function useTemplate(template: AccessPolicyTemplateDto) {
  templatesVisible.value = false
  void openCreate(template)
}

// ── 新增 / 编辑 ─────────────────────────────────────────────────
const modalVisible = ref(false)
const submitLoading = ref(false)
const form = ref<PolicyFormModel>(emptyForm())
const groups = ref<ConditionGroupModel[]>([])
const modalTitle = computed(() => form.value.basicId ? t('setting.access_policy.modal_title_edit') : t('setting.access_policy.modal_title_add'))
const usesPlaceholderNetwork = computed(() => toConditions(groups.value, catalog.value).some(condition => condition.conditionValue.includes('203.0.113.0/24')))

function emptyForm(): PolicyFormModel {
  return {
    policyCode: '',
    policyName: '',
    description: null,
    priority: 100,
    status: EnableStatus.Enabled,
    effectiveTime: null,
    expirationTime: null,
    templateCode: null,
    remark: null,
  }
}

async function openCreate(template: AccessPolicyTemplateDto | null) {
  if (!await ensureCatalog()) {
    return
  }
  form.value = template
    ? { ...emptyForm(), policyCode: template.code.replace(/^preset\./, ''), policyName: template.name, description: template.description, priority: template.priority, templateCode: template.code }
    : emptyForm()
  groups.value = toGroups(template?.conditions ?? [], catalog.value)
  modalVisible.value = true
}

async function openEdit(row: AccessPolicyListItemDto) {
  if (!await ensureCatalog()) {
    return
  }
  try {
    const current = await accessPolicyApi.detail(row.basicId)
    if (!current) {
      toast.warning(t('setting.access_policy.not_found'))
      return
    }
    form.value = {
      basicId: current.basicId,
      policyCode: current.policyCode,
      policyName: current.policyName,
      description: current.description ?? null,
      priority: current.priority,
      status: current.status,
      effectiveTime: current.effectiveTime ? new Date(current.effectiveTime).getTime() : null,
      expirationTime: current.expirationTime ? new Date(current.expirationTime).getTime() : null,
      templateCode: current.templateCode ?? null,
      remark: current.remark ?? null,
    }
    groups.value = toGroups(current.conditions, catalog.value)
    modalVisible.value = true
  }
  catch (error) {
    toast.danger((error as Error).message || t('setting.access_policy.load_detail_failed'))
  }
}

function validate() {
  const current = form.value
  if (!current.basicId && !current.policyCode.trim()) {
    toast.warning(t('setting.access_policy.warn_code_required'))
    return false
  }
  if (!current.basicId && /\s/.test(current.policyCode.trim())) {
    toast.warning(t('setting.access_policy.warn_code_whitespace'))
    return false
  }
  if (!current.policyName.trim()) {
    toast.warning(t('setting.access_policy.warn_name_required'))
    return false
  }
  if (current.effectiveTime != null && current.expirationTime != null && current.effectiveTime >= current.expirationTime) {
    toast.warning(t('setting.access_policy.warn_time_order'))
    return false
  }
  const incomplete = findIncomplete(groups.value, catalog.value)
  if (incomplete) {
    toast.warning(t(`setting.access_policy.editor.${incomplete.key}`, { index: incomplete.index }))
    return false
  }
  return true
}

async function handleSubmit() {
  if (!validate()) {
    return
  }
  const current = form.value
  const shared = {
    policyName: current.policyName.trim(),
    description: normalizeNullable(current.description),
    priority: current.priority ?? 100,
    effectiveTime: current.effectiveTime != null ? new Date(current.effectiveTime).toISOString() : null,
    expirationTime: current.expirationTime != null ? new Date(current.expirationTime).toISOString() : null,
    remark: normalizeNullable(current.remark),
    conditions: toConditions(groups.value, catalog.value),
  }
  submitLoading.value = true
  try {
    if (current.basicId) {
      await accessPolicyApi.update({ basicId: current.basicId, ...shared })
    }
    else {
      await accessPolicyApi.create({ ...shared, policyCode: current.policyCode.trim(), status: current.status, templateCode: current.templateCode })
    }
    toast.success(t('setting.access_policy.saved'))
    modalVisible.value = false
    reload()
  }
  catch (error) {
    toast.danger((error as Error).message || t('setting.access_policy.save_failed'))
  }
  finally {
    submitLoading.value = false
  }
}

// ── 试运行 ──────────────────────────────────────────────────────
const trialVisible = ref(false)
const trialTarget = ref<{ title: string, policyId: ApiId | null, conditions: AccessPolicyConditionDto[] | null, action: AccessPolicyAction }>({
  title: '',
  policyId: null,
  conditions: null,
  action: AccessPolicyAction.LogOnly,
})

function openTrial(target: typeof trialTarget.value) {
  trialTarget.value = target
  trialVisible.value = true
}

function trialDraft() {
  const incomplete = findIncomplete(groups.value, catalog.value)
  if (incomplete) {
    toast.warning(t(`setting.access_policy.editor.${incomplete.key}`, { index: incomplete.index }))
    return
  }
  openTrial({
    title: form.value.policyName.trim() || t('setting.access_policy.draft'),
    policyId: null,
    conditions: toConditions(groups.value, catalog.value),
    action: AccessPolicyAction.Deny,
  })
}

// ── 改动作 / 启停 / 删除 ────────────────────────────────────────
function confirmAction(row: AccessPolicyListItemDto, action: AccessPolicyAction) {
  const content = {
    [AccessPolicyAction.Deny]: t('setting.access_policy.confirm_enable_blocking', { name: row.policyName }),
    [AccessPolicyAction.Allow]: t('setting.access_policy.confirm_set_allow', { name: row.policyName }),
    [AccessPolicyAction.LogOnly]: t('setting.access_policy.confirm_set_log_only', { name: row.policyName }),
  }[action]
  void dialog.confirm({
    badge: 'warning',
    tone: action === AccessPolicyAction.Deny ? 'danger' : undefined,
    title: getOptionLabel(actionOptions.value, action),
    content,
    okText: t('setting.access_policy.confirm'),
    cancelText: t('setting.access_policy.cancel'),
    onOk: async () => {
      try {
        await accessPolicyApi.updateAction({ basicId: row.basicId, action })
        toast.success(t('setting.access_policy.action_changed'))
        reload()
      }
      catch (error) {
        toast.danger((error as Error).message || t('setting.access_policy.save_failed'))
      }
    },
  })
}

function confirmToggle(row: AccessPolicyListItemDto) {
  const enable = row.status !== EnableStatus.Enabled
  void dialog.confirm({
    badge: 'warning',
    title: enable ? t('setting.access_policy.enable_title') : t('setting.access_policy.disable_title'),
    content: enable ? t('setting.access_policy.confirm_enable', { name: row.policyName }) : t('setting.access_policy.confirm_disable', { name: row.policyName }),
    okText: t('setting.access_policy.confirm'),
    cancelText: t('setting.access_policy.cancel'),
    onOk: async () => {
      try {
        await accessPolicyApi.updateStatus({ basicId: row.basicId, status: enable ? EnableStatus.Enabled : EnableStatus.Disabled })
        toast.success(t('setting.access_policy.status_changed'))
        reload()
      }
      catch (error) {
        toast.danger((error as Error).message || t('setting.access_policy.save_failed'))
      }
    },
  })
}

function confirmDelete(row: AccessPolicyListItemDto) {
  void dialog.confirm({
    badge: 'error',
    tone: 'danger',
    title: t('setting.access_policy.delete_title'),
    content: t('setting.access_policy.confirm_delete', { name: row.policyName }),
    okText: t('setting.access_policy.action_delete'),
    cancelText: t('setting.access_policy.cancel'),
    onOk: async () => {
      try {
        await accessPolicyApi.delete(row.basicId)
        toast.success(t('setting.access_policy.deleted'))
        reload()
      }
      catch (error) {
        toast.danger((error as Error).message || t('setting.access_policy.delete_failed'))
      }
    },
  })
}

// ── 自动封禁 ────────────────────────────────────────────────────
const bansVisible = ref(false)
const canUnban = computed(() => hasPermission('setting.access-policy.ban'))
const canEditAutoBan = computed(() => hasPermission('setting.config.update'))

function openBans() {
  bansVisible.value = true
}
</script>

<template>
  <SchemaPage ref="schemaPageRef" :schema="schema" @action="onAction">
    <!-- 详情 -->
    <XhDrawerRoot v-model:open="detailVisible" side="right">
      <XhDrawerContent style="--xh-drawer-size: 820px">
        <XhDrawerHeader>
          <XhDrawerTitle>{{ detail?.policyName }}</XhDrawerTitle>
        </XhDrawerHeader>
        <XhDrawerCloseTrigger />
        <XhDrawerBody v-if="detail" class="xh-overlay-body policy-detail">
          <XhDescriptionsRoot :columns="2" variant="outline" size="sm">
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('setting.access_policy.policy_code') }}</XhDescriptionsLabel>
              <XhDescriptionsValue><code>{{ detail.policyCode }}</code></XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('setting.access_policy.action') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ getOptionLabel(actionOptions, detail.action) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('setting.access_policy.phase') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ getOptionLabel(phaseOptions, detail.phase) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('setting.access_policy.priority') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ detail.priority }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('setting.access_policy.status') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ getOptionLabel(statusOptions, detail.status) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('setting.access_policy.is_active') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ detail.isActive ? t('setting.access_policy.active_yes') : t('setting.access_policy.active_no') }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('setting.access_policy.effective_range') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ detail.effectiveTime ? formatDate(detail.effectiveTime) : '-' }} ~ {{ detail.expirationTime ? formatDate(detail.expirationTime) : '-' }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('setting.access_policy.recent_hits') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ detail.recentHitCount }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('setting.access_policy.description') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ detail.description || '-' }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('setting.access_policy.remark') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ detail.remark || '-' }}</XhDescriptionsValue>
            </XhDescriptionsItem>
          </XhDescriptionsRoot>
          <table class="xh-detail-table">
            <caption class="policy-detail__caption">
              {{ t('setting.access_policy.editor.title') }}
            </caption>
            <thead>
              <tr>
                <th>{{ t('setting.access_policy.th_group') }}</th>
                <th>{{ t('identity.condition.th_attribute') }}</th>
                <th>{{ t('identity.condition.th_operator') }}</th>
                <th>{{ t('identity.condition.th_value') }}</th>
              </tr>
            </thead>
            <tbody>
              <tr v-for="(condition, index) in detail.conditions" :key="index">
                <td>{{ condition.conditionGroup + 1 }}</td>
                <td><code>{{ condition.attributeName }}</code></td>
                <td>{{ condition.isNegated ? t('identity.condition.negated_prefix') : '' }}{{ getOptionLabel(operatorOptions, condition.operator) }}</td>
                <td><code>{{ condition.conditionValue || '-' }}</code></td>
              </tr>
            </tbody>
          </table>
        </XhDrawerBody>
      </XhDrawerContent>
    </XhDrawerRoot>

    <!-- 从模板新建 -->
    <XhDrawerRoot v-model:open="templatesVisible" side="right">
      <XhDrawerContent style="--xh-drawer-size: 640px">
        <XhDrawerHeader>
          <XhDrawerTitle>{{ t('setting.access_policy.template_title') }}</XhDrawerTitle>
        </XhDrawerHeader>
        <XhDrawerCloseTrigger />
        <XhDrawerBody class="xh-overlay-body policy-templates">
          <p class="policy-templates__tip">
            {{ t('setting.access_policy.template_tip') }}
          </p>
          <article v-for="template in templates" :key="template.code" class="policy-template">
            <header class="policy-template__head">
              <span class="policy-template__name">{{ template.name }}</span>
              <XhTagRoot variant="subtle" size="sm" :tone="actionTone(template.suggestedAction)">
                <XhTagLabel>{{ t('setting.access_policy.template_suggested', { action: getOptionLabel(actionOptions, template.suggestedAction) }) }}</XhTagLabel>
              </XhTagRoot>
            </header>
            <p class="policy-template__description">
              {{ template.description }}
            </p>
            <div>
              <XhButton variant="subtle" size="sm" tone="brand" @click="useTemplate(template)">
                <XhButtonLabel>{{ t('setting.access_policy.template_use') }}</XhButtonLabel>
              </XhButton>
            </div>
          </article>
        </XhDrawerBody>
      </XhDrawerContent>
    </XhDrawerRoot>

    <!-- 新增 / 编辑 -->
    <XEditModal v-model:show="modalVisible" :title="modalTitle" :loading="submitLoading" :form-id="editFormId" width="1080px">
      <XhAlertRoot v-if="!form.basicId" tone="info" variant="subtle" class="policy-form__alert">
        <XhAlertIndicator>
          <Icon icon="lucide:info" width="16" />
        </XhAlertIndicator>
        <XhAlertContent>
          <XhAlertDescription>{{ t('setting.access_policy.hint_log_only') }}</XhAlertDescription>
        </XhAlertContent>
      </XhAlertRoot>
      <XhAlertRoot v-if="usesPlaceholderNetwork" tone="warning" variant="subtle" class="policy-form__alert">
        <XhAlertIndicator>
          <Icon icon="lucide:triangle-alert" width="16" />
        </XhAlertIndicator>
        <XhAlertContent>
          <XhAlertDescription>{{ t('setting.access_policy.hint_placeholder_network') }}</XhAlertDescription>
        </XhAlertContent>
      </XhAlertRoot>
      <XhFormRoot :id="editFormId" v-model:values="form" validate-on="blur" class="xh-edit-form-grid" @submit="handleSubmit">
        <XhFormFieldGroup name="policyCode">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('setting.access_policy.policy_code') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="form.policyCode" clearable :disabled="Boolean(form.basicId)" :placeholder="t('setting.access_policy.ph_policy_code')" />
            </XhFieldControl>
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup name="policyName">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('setting.access_policy.policy_name') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="form.policyName" clearable :placeholder="t('setting.access_policy.ph_policy_name')" />
            </XhFieldControl>
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup name="priority">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('setting.access_policy.priority') }}</XhFieldLabel>
            <XhFieldControl>
              <XNumberInput v-model:value="form.priority" :min="0" />
            </XhFieldControl>
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup name="status">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('setting.access_policy.status') }}</XhFieldLabel>
            <XhFieldControl>
              <!-- 编辑时启停走单独的操作 -->
              <XSelect v-model:value="form.status" :options="statusOptions" :disabled="Boolean(form.basicId)" />
            </XhFieldControl>
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup name="effectiveTime">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('setting.access_policy.effective_time') }}</XhFieldLabel>
            <XhFieldControl>
              <XDatePicker v-model:value="form.effectiveTime" clearable show-time />
            </XhFieldControl>
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup name="expirationTime">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('setting.access_policy.expiration_time') }}</XhFieldLabel>
            <XhFieldControl>
              <XDatePicker v-model:value="form.expirationTime" clearable show-time />
            </XhFieldControl>
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup name="description" class="xh-span-2">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('setting.access_policy.description') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="form.description" clearable type="textarea" :rows="2" />
            </XhFieldControl>
          </XhFieldRoot>
        </XhFormFieldGroup>
        <XhFormFieldGroup name="remark" class="xh-span-2">
          <XhFieldRoot>
            <XhFieldLabel>{{ t('setting.access_policy.remark') }}</XhFieldLabel>
            <XhFieldControl>
              <XInput v-model:value="form.remark" clearable />
            </XhFieldControl>
          </XhFieldRoot>
        </XhFormFieldGroup>
      </XhFormRoot>
      <AccessPolicyConditionEditor v-model:groups="groups" :catalog="catalog" />
      <div v-if="hasPermission('setting.access-policy.trial')" class="policy-form__trial">
        <XhButton variant="outline" size="sm" @click="trialDraft">
          <XhButtonLabel>{{ t('setting.access_policy.trial_draft') }}</XhButtonLabel>
        </XhButton>
      </div>
    </XEditModal>

    <AccessPolicyTrialDrawer
      v-model:open="trialVisible"
      :title="trialTarget.title"
      :policy-id="trialTarget.policyId"
      :conditions="trialTarget.conditions"
      :action="trialTarget.action"
    />
    <LoginBanDrawer v-if="isPlatform" v-model:open="bansVisible" :can-unban="canUnban" :can-edit-settings="canEditAutoBan" />
  </SchemaPage>
</template>

<style scoped>
.policy-detail,
.policy-templates {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-3);
}

.policy-detail__caption {
  padding-block-end: var(--xh-space-1);
  font-weight: var(--xh-font-weight-medium);
  text-align: start;
}

.policy-templates__tip,
.policy-template__description {
  margin: 0;
  color: var(--xh-fg-muted);
  font-size: var(--xh-text-caption-size);
}

.policy-template {
  display: flex;
  flex-direction: column;
  gap: var(--xh-space-2);
  padding: var(--xh-space-3);
  border: 1px solid var(--xh-border-default);
  border-radius: var(--xh-radius-md);
}

.policy-template__head {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: var(--xh-space-2);
}

.policy-template__name {
  font-weight: var(--xh-font-weight-medium);
}

.policy-form__alert {
  margin-block-end: var(--xh-space-2);
}

.policy-form__trial {
  display: flex;
  justify-content: flex-end;
  margin-block-start: var(--xh-space-2);
}
</style>
