<!--
  系统设置 / 业务编号页面。
  职责：两侧都有——平台看平台规则，租户看自己的规则与供租户使用的平台规则（另一侧的只读）；规则管理、格式预览、安全重置和永久发号记录；页面不提供真实发号入口。
-->
<script lang="ts" setup>
import type { DateFormat, ResetCycle, RuleRow, RuleStatus } from './use-numbering-rules'
import type { ListFieldSchema, PageSchema, SchemaActionPayload, SchemaPageInstance } from '~/components'
import type { SelectOption } from '~/types'
import { XhAlertContent, XhAlertDescription, XhAlertIndicator, XhAlertRoot, XhButton, XhButtonIndicator, XhButtonLabel, XhDescriptionsItem, XhDescriptionsLabel, XhDescriptionsRoot, XhDescriptionsValue, XhDialogBody, XhDialogCloseTrigger, XhDialogContent, XhDialogFooter, XhDialogHeader, XhDialogRoot, XhDialogTitle, XhFieldControl, XhFieldErrorText, XhFieldLabel, XhFieldRoot, XhFlex, XhFormRoot, XhSwitch, XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { computed, ref, useId } from 'vue'
import { useI18n } from 'vue-i18n'
import { deleteConfirmText, Icon, renderTag, SchemaPage, statusConfirmText, XEditModal, XInput, XNumberInput, XSelect, XTooltip } from '~/components'
import TimezoneSwitcher from '~/components/common/TimezoneSwitcher.vue'
import { toast } from '~/composables'
import { useAuthStore } from '~/stores'
import { formatDate } from '~/utils'
import NumberingAllocationDrawer from './components/NumberingAllocationDrawer.vue'
import NumberingPreviewModal from './components/NumberingPreviewModal.vue'
import { DATE_FORMATS, datePattern, numberingRulesResource, RESET_CYCLES, useNumberingReset, useNumberingRuleEditor } from './use-numbering-rules'

defineOptions({ name: 'SettingsNumberingRulesView' })

const { t } = useI18n()
const auth = useAuthStore()
const isPlatformCaller = () => auth.tenantId === null
const editor = useNumberingRuleEditor(isPlatformCaller)
const resetter = useNumberingReset()
const resource = numberingRulesResource()

/** 编辑弹窗的保存钮靠这个 id 关联到表单，点它才会走整表提交 */
const editFormId = useId()
const resetFormId = useId()
const schemaPageRef = ref<SchemaPageInstance | null>(null)

function reload() {
  void schemaPageRef.value?.reload()
}

// 枚举值是契约字面量，文案键静态写出以便 i18n 门禁核对
function cycleLabel(cycle: ResetCycle): string {
  switch (cycle) {
    case 'Daily': return t('settings.numbering_rules.cycles.daily')
    case 'Monthly': return t('settings.numbering_rules.cycles.monthly')
    case 'Yearly': return t('settings.numbering_rules.cycles.yearly')
    default: return t('settings.numbering_rules.cycles.never')
  }
}

function dateFormatLabel(format: DateFormat): string {
  return format === 'None' ? t('settings.numbering_rules.no_date') : datePattern(format)
}

function statusLabel(status: RuleStatus): string {
  return status === 'Disabled' ? t('settings.numbering_rules.status.disabled') : t('settings.numbering_rules.status.active')
}

function ownerLabel(row: RuleRow): string {
  return row.isPlatform ? t('settings.numbering_rules.owner.platform') : t('settings.numbering_rules.owner.tenant')
}

const dateFormatOptions = computed<SelectOption[]>(() => DATE_FORMATS.map(format => ({ label: dateFormatLabel(format), value: format })))
const resetCycleOptions = computed<SelectOption[]>(() => RESET_CYCLES.map(cycle => ({ label: cycleLabel(cycle), value: cycle })))
const statusOptions = computed<SelectOption[]>(() => (['Active', 'Disabled'] as const).map(status => ({ label: statusLabel(status), value: status })))
const ownerOptions = computed(() => [
  { label: t('settings.numbering_rules.owner.platform'), value: 'platform' },
  { label: t('settings.numbering_rules.owner.tenant'), value: 'tenant' },
])

// ── 字段单一事实源（列 + 搜索；仅搜索 / 导入字段 visible:false；order 控顺序；可排序的只放后端白名单里的） ──
// SchemaPage 的行类型在框架边界放宽（Record）：回调里按本页的行类型取用
const asRow = (row: object) => row as RuleRow

const fields = computed<ListFieldSchema[]>(() => [
  { key: 'keyword', title: t('settings.numbering_rules.keyword'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('settings.numbering_rules.keyword_placeholder'), order: 0 },
  { key: 'code', title: t('settings.numbering_rules.fields.code'), dataType: 'string', sortable: true, importable: true, required: true, exportable: true, minWidth: 150, order: 1 },
  { key: 'name', title: t('settings.numbering_rules.fields.name'), dataType: 'string', sortable: true, importable: true, required: true, exportable: true, minWidth: 140, order: 2 },
  { key: 'template', title: t('settings.numbering_rules.fields.template'), dataType: 'string', exportable: true, minWidth: 210, order: 3 },
  { key: 'prefix', title: t('settings.numbering_rules.fields.prefix'), dataType: 'string', visible: false, importable: true, order: 3.1 },
  { key: 'separator', title: t('settings.numbering_rules.fields.separator'), dataType: 'string', visible: false, importable: true, order: 3.2 },
  { key: 'dateFormat', title: t('settings.numbering_rules.fields.date_format'), dataType: 'enum', visible: false, importable: true, options: dateFormatOptions.value, order: 3.3 },
  { key: 'serialLength', title: t('settings.numbering_rules.fields.serial_length'), dataType: 'number', visible: false, importable: true, order: 3.4 },
  { key: 'resetCycle', title: t('settings.numbering_rules.fields.reset_cycle'), dataType: 'enum', advancedSearch: true, importable: true, exportable: true, options: resetCycleOptions.value, render: row => cycleLabel(asRow(row).resetCycle), ellipsis: true, width: 100, order: 4 },
  { key: 'timeZoneId', title: t('settings.numbering_rules.fields.time_zone'), dataType: 'string', importable: true, exportable: true, width: 140, order: 5 },
  { key: 'currentPeriod', title: t('settings.numbering_rules.fields.current_period'), dataType: 'string', render: row => asRow(row).currentPeriod || '-', ellipsis: true, width: 110, order: 6 },
  { key: 'currentValue', title: t('settings.numbering_rules.fields.current_value'), dataType: 'number', exportable: true, width: 120, order: 7 },
  { key: 'owner', title: t('settings.numbering_rules.fields.owner'), dataType: 'enum', searchable: true, options: ownerOptions.value, render: row => ownerLabel(asRow(row)), width: 90, order: 8 },
  // 只有平台规则能供租户使用：租户这一侧这一列没有意义
  { key: 'allowTenantUse', title: t('settings.numbering_rules.fields.allow_tenant_use'), dataType: 'boolean', visible: isPlatformCaller(), width: 110, order: 9 },
  { key: 'status', title: t('settings.numbering_rules.fields.status'), dataType: 'enum', searchable: true, exportable: true, options: statusOptions.value, render: row => renderTag(statusLabel(asRow(row).status), asRow(row).status === 'Active' ? 'success' : 'danger'), width: 90, order: 10 },
  { key: 'order', title: t('settings.numbering_rules.fields.order'), dataType: 'number', sortable: true, importable: true, exportable: true, width: 80, order: 11 },
  { key: 'description', title: t('settings.numbering_rules.fields.description'), dataType: 'text', visible: false, importable: true, order: 11.5 },
])

const schema = computed<PageSchema>(() => ({
  pageCode: 'settings.numbering-rules',
  pageName: t('settings.numbering_rules.title'),
  exportPermission: 'settings.numbering-rules.read',
  importPermission: 'settings.numbering-rules.create',
  batchRemovable: true,
  removePermission: 'settings.numbering-rules.delete',
  statusPermission: 'settings.numbering-rules.update',
  rowKey: 'id',
  fields: fields.value,
  resource: resource as unknown as PageSchema['resource'],
  actions: [
    { key: 'create', title: t('settings.numbering_rules.actions.create'), scope: 'page', tone: 'brand', icon: 'lucide:plus', permission: 'settings.numbering-rules.create' },
    { key: 'preview', title: t('settings.numbering_rules.actions.preview'), scope: 'row', icon: 'lucide:scan-eye', permission: 'settings.numbering-rules.read' },
    { key: 'view', title: t('common.actions.view'), scope: 'row', icon: 'lucide:eye' },
    // 另一侧的规则只读：租户看得见供租户使用的平台规则，改不了
    { key: 'edit', title: t('common.actions.edit'), scope: 'row', icon: 'lucide:pencil', visible: row => editor.isOwn(asRow(row)), permission: 'settings.numbering-rules.update' },
    { key: 'toggle', title: t('settings.numbering_rules.actions.toggle'), scope: 'row', icon: 'lucide:power', confirm: true, confirmText: row => statusConfirmText(t, asRow(row).status === 'Active', asRow(row).name), visible: row => editor.isOwn(asRow(row)), permission: 'settings.numbering-rules.update' },
    { key: 'reset', title: t('settings.numbering_rules.actions.reset'), scope: 'row', tone: 'warning', icon: 'lucide:rotate-ccw', visible: row => editor.isOwn(asRow(row)), permission: 'settings.numbering-rules.reset' },
    { key: 'allocations', title: t('settings.numbering_rules.actions.allocations'), scope: 'row', icon: 'lucide:history', permission: 'settings.numbering-rules.read' },
    // 发过号的规则不能删（发出的号永久记录）
    { key: 'delete', title: t('common.actions.delete'), scope: 'row', tone: 'danger', icon: 'lucide:trash-2', confirm: true, confirmText: row => deleteConfirmText(t, asRow(row).name), visible: row => editor.isOwn(asRow(row)), disabled: row => asRow(row).hasAllocated, permission: 'settings.numbering-rules.delete' },
  ],
}))

const allocationVisible = ref(false)
const allocationRule = ref<RuleRow | null>(null)
const previewVisible = ref(false)
const previewRule = ref<RuleRow | null>(null)

// ── 行 / 页面操作分发 ──
function onAction(payload: SchemaActionPayload) {
  const row = payload.row ? asRow(payload.row) : undefined
  switch (payload.key) {
    case 'create':
      editor.startCreate()
      break
    case 'preview':
      if (row) {
        previewRule.value = row
        previewVisible.value = true
      }
      break
    case 'view':
      if (row)
        openDetail(row)
      break
    case 'edit':
      if (row)
        editor.startEdit(row)
      break
    case 'toggle':
      if (row)
        void toggleStatus(row)
      break
    case 'reset':
      if (row)
        resetter.start(row)
      break
    case 'allocations':
      if (row) {
        allocationRule.value = row
        allocationVisible.value = true
      }
      break
    case 'delete':
      if (row)
        void removeRow(row)
      break
  }
}

async function onSubmit() {
  if (await editor.submit()) {
    toast.success(t('common.messages.save_success'))
    reload()
  }
}

async function onPreviewInEditor() {
  try {
    await editor.runPreview()
  }
  catch (error) {
    toast.danger((error as Error).message || t('common.messages.operation_failed'))
  }
}

async function onResetSubmit() {
  if (await resetter.submit()) {
    toast.success(t('settings.numbering_rules.reset_success'))
    reload()
  }
}

async function removeRow(row: RuleRow) {
  try {
    await resource.remove?.(row.id)
    toast.success(t('common.messages.delete_success'))
    reload()
  }
  catch (error) {
    toast.danger((error as Error).message || t('common.messages.delete_failed'))
  }
}

async function toggleStatus(row: RuleRow) {
  try {
    await resource.updateStatus?.(row.id, row.status !== 'Active')
    toast.success(t('common.messages.status_updated'))
    reload()
  }
  catch (error) {
    toast.danger((error as Error).message || t('common.messages.status_failed'))
  }
}

// ── 查看详情 ──
const detailOpen = ref(false)
const detail = ref<RuleRow | null>(null)

function openDetail(row: RuleRow) {
  detail.value = row
  detailOpen.value = true
}

function formatNullable(value: unknown): string {
  return value === null || value === undefined || value === '' ? '-' : String(value)
}

function formatBoolean(value: boolean): string {
  return value ? t('common.statuses.yes') : t('common.statuses.no')
}
</script>

<template>
  <SchemaPage
    ref="schemaPageRef"
    :schema="schema"
    @action="onAction"
  >
    <NumberingPreviewModal v-model:show="previewVisible" :rule="previewRule" />
    <NumberingAllocationDrawer v-model:show="allocationVisible" :rule="allocationRule" />

    <XhDialogRoot v-model:open="detailOpen">
      <XhDialogContent class="xh-mgmt-detail-modal" style="--xh-dialog-max-w: 720px">
        <XhDialogHeader>
          <XhDialogTitle>
            <div v-if="detail" class="det-hd-entity">
              <div class="det-hd-ico">
                <Icon icon="lucide:hash" width="22" height="22" />
              </div>
              <div class="min-w-0">
                <div class="det-hd-name">
                  {{ detail.name }}
                </div>
                <div class="det-hd-sub">
                  {{ detail.code }}
                </div>
              </div>
            </div>
          </XhDialogTitle>
        </XhDialogHeader>
        <XhDialogCloseTrigger />
        <XhDialogBody class="xh-overlay-body">
          <XhDescriptionsRoot v-if="detail" :columns="2" variant="outline" size="sm">
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('settings.numbering_rules.fields.owner') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ ownerLabel(detail) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('settings.numbering_rules.fields.status') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ statusLabel(detail.status) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('settings.numbering_rules.fields.template') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ detail.template }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('settings.numbering_rules.fields.date_format') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ dateFormatLabel(detail.dateFormat) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('settings.numbering_rules.fields.prefix') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ formatNullable(detail.prefix) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('settings.numbering_rules.fields.separator') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ formatNullable(detail.separator) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('settings.numbering_rules.fields.serial_length') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ detail.serialLength }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('settings.numbering_rules.fields.reset_cycle') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ cycleLabel(detail.resetCycle) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('settings.numbering_rules.fields.time_zone') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ detail.timeZoneId }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('settings.numbering_rules.fields.current_period') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ formatNullable(detail.currentPeriod) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('settings.numbering_rules.fields.current_value') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ detail.currentValue }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('settings.numbering_rules.fields.has_allocated') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ formatBoolean(detail.hasAllocated) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('settings.numbering_rules.fields.allow_tenant_use') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ formatBoolean(detail.allowTenantUse) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('settings.numbering_rules.fields.order') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ detail.order }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('settings.numbering_rules.fields.created_at') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ formatDate(detail.createdAt) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <XhDescriptionsItem>
              <XhDescriptionsLabel>{{ t('settings.numbering_rules.fields.modified_at') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ detail.modifiedAt ? formatDate(detail.modifiedAt) : '-' }}</XhDescriptionsValue>
            </XhDescriptionsItem>
            <!-- 跨列项排在末尾：边框按子节点奇偶画竖线，跨列项之后的项奇偶会与实际列位错开 -->
            <XhDescriptionsItem style="grid-column: span 2">
              <XhDescriptionsLabel>{{ t('settings.numbering_rules.fields.description') }}</XhDescriptionsLabel>
              <XhDescriptionsValue>{{ formatNullable(detail.description) }}</XhDescriptionsValue>
            </XhDescriptionsItem>
          </XhDescriptionsRoot>
        </XhDialogBody>
        <XhDialogFooter>
          <XhButton variant="subtle" size="sm" @click="detailOpen = false">
            {{ t('common.actions.close') }}
          </XhButton>
        </XhDialogFooter>
      </XhDialogContent>
    </XhDialogRoot>

    <!-- 规则 新增/编辑：发过号后格式冻结，只能改名称、说明、状态与是否供租户使用 -->
    <XEditModal
      v-model:show="editor.open.value"
      :title="editor.isEdit.value ? t('settings.numbering_rules.edit_title') : t('settings.numbering_rules.create_title')"
      :loading="editor.submitting.value"
      :form-id="editFormId"
    >
      <XhAlertRoot v-if="editor.formatFrozen.value" tone="warning" class="mb-3">
        <XhAlertIndicator>
          <Icon icon="lucide:triangle-alert" width="16" height="16" />
        </XhAlertIndicator>
        <XhAlertContent>
          <XhAlertDescription>
            {{ t('settings.numbering_rules.format_frozen_tip') }}
          </XhAlertDescription>
        </XhAlertContent>
      </XhAlertRoot>
      <XhFormRoot :id="editFormId" class="xh-edit-form-grid" data-testid="numbering-rule-form" @submit="onSubmit">
        <XhFieldRoot :invalid="!!editor.fieldError('code')" :required="!editor.isEdit.value">
          <XhFieldLabel>{{ t('settings.numbering_rules.fields.code') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="editor.form.code" name="code" clearable autocomplete="off" :max-length="100" :disabled="editor.isEdit.value" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('code')">
            {{ editor.fieldError('code') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('name')" required>
          <XhFieldLabel>{{ t('settings.numbering_rules.fields.name') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="editor.form.name" name="name" clearable :max-length="100" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('name')">
            {{ editor.fieldError('name') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('prefix')">
          <XhFieldLabel>{{ t('settings.numbering_rules.fields.prefix') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="editor.form.prefix" name="prefix" autocomplete="off" :max-length="50" :disabled="editor.formatFrozen.value" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('prefix')">
            {{ editor.fieldError('prefix') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('separator')">
          <XhFieldLabel>{{ t('settings.numbering_rules.fields.separator') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="editor.form.separator" name="separator" autocomplete="off" :max-length="10" :disabled="editor.formatFrozen.value" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('separator')">
            {{ editor.fieldError('separator') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('dateFormat')">
          <XhFieldLabel>{{ t('settings.numbering_rules.fields.date_format') }}</XhFieldLabel>
          <XhFieldControl>
            <XSelect v-model:value="editor.form.dateFormat" name="dateFormat" :options="dateFormatOptions" :disabled="editor.formatFrozen.value" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('dateFormat')">
            {{ editor.fieldError('dateFormat') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('serialLength')">
          <XhFieldLabel>{{ t('settings.numbering_rules.fields.serial_length') }}</XhFieldLabel>
          <XhFieldControl>
            <XNumberInput v-model:value="editor.form.serialLength" name="serialLength" :min="1" :max="18" :precision="0" :disabled="editor.formatFrozen.value" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('serialLength')">
            {{ editor.fieldError('serialLength') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('resetCycle')">
          <XhFieldLabel>{{ t('settings.numbering_rules.fields.reset_cycle') }}</XhFieldLabel>
          <XhFieldControl>
            <XSelect v-model:value="editor.form.resetCycle" name="resetCycle" :options="resetCycleOptions" :disabled="editor.formatFrozen.value" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('resetCycle')">
            {{ editor.fieldError('resetCycle') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('timeZoneId')">
          <XhFieldLabel>{{ t('settings.numbering_rules.fields.time_zone') }}</XhFieldLabel>
          <XhFieldControl>
            <!-- 四百多条目录须可检索；只收非空串，表单值始终是字符串 -->
            <TimezoneSwitcher v-model:value="editor.form.timeZoneId" size="sm" :disabled="editor.formatFrozen.value" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('timeZoneId')">
            {{ editor.fieldError('timeZoneId') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot v-if="isPlatformCaller()">
          <XhFieldLabel>
            <span class="inline-flex items-center gap-1">
              <span>{{ t('settings.numbering_rules.fields.allow_tenant_use') }}</span>
              <XTooltip :content="t('settings.numbering_rules.allow_tenant_use_tip')">
                <button type="button" class="inline-flex cursor-help items-center text-fg-muted" :aria-label="t('settings.numbering_rules.allow_tenant_use_tip')">
                  <Icon icon="lucide:circle-help" width="14" height="14" />
                </button>
              </XTooltip>
            </span>
          </XhFieldLabel>
          <XhFieldControl>
            <XhSwitch v-model:checked="editor.form.allowTenantUse" name="allowTenantUse" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot v-if="editor.isEdit.value">
          <XhFieldLabel>{{ t('settings.numbering_rules.fields.status') }}</XhFieldLabel>
          <XhFieldControl>
            <XSelect v-model:value="editor.form.status" name="status" :options="statusOptions" />
          </XhFieldControl>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!editor.fieldError('order')">
          <XhFieldLabel>{{ t('settings.numbering_rules.fields.order') }}</XhFieldLabel>
          <XhFieldControl>
            <XNumberInput v-model:value="editor.form.order" name="order" :min="0" :precision="0" />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('order')">
            {{ editor.fieldError('order') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot class="xh-span-2" :invalid="!!editor.fieldError('description')">
          <XhFieldLabel>{{ t('settings.numbering_rules.fields.description') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="editor.form.description" name="description" type="textarea" :rows="2" :max-length="500" clearable />
          </XhFieldControl>
          <XhFieldErrorText v-if="editor.fieldError('description')">
            {{ editor.fieldError('description') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot class="xh-span-2">
          <XhFieldLabel>{{ t('settings.numbering_rules.editor_preview') }}</XhFieldLabel>
          <!-- 按钮加结果标签不是一枚控件，不套 XhFieldControl -->
          <XhFlex align="center">
            <XhButton variant="subtle" size="sm" :loading="editor.previewing.value" @click="onPreviewInEditor">
              <XhButtonIndicator />
              <XhButtonLabel>{{ t('settings.numbering_rules.editor_preview_action') }}</XhButtonLabel>
            </XhButton>
            <XhTagRoot v-if="editor.previewNumber.value" variant="subtle" tone="info" size="lg">
              <XhTagLabel>{{ editor.previewNumber.value }}</XhTagLabel>
            </XhTagRoot>
          </XhFlex>
        </XhFieldRoot>
        <p v-if="editor.submitError.value" class="xh-span-2 text-caption text-fg-danger" role="alert">
          {{ editor.submitError.value }}
        </p>
      </XhFormRoot>
    </XEditModal>

    <!-- 安全重置：下一个值要大于本周期已发出的最大值（可向前跳、不能回退）；平台规则回填编码确认 -->
    <XEditModal
      v-model:show="resetter.open.value"
      :title="t('settings.numbering_rules.reset_title')"
      :loading="resetter.submitting.value"
      :form-id="resetFormId"
    >
      <XhAlertRoot tone="warning" class="mb-3">
        <XhAlertIndicator>
          <Icon icon="lucide:triangle-alert" width="16" height="16" />
        </XhAlertIndicator>
        <XhAlertContent>
          <XhAlertDescription>
            {{ t('settings.numbering_rules.reset_tip') }}
          </XhAlertDescription>
        </XhAlertContent>
      </XhAlertRoot>
      <XhFormRoot :id="resetFormId" data-testid="numbering-reset-form" @submit="onResetSubmit">
        <XhFieldRoot :invalid="!!resetter.fieldError('nextValue')" required>
          <XhFieldLabel>{{ t('settings.numbering_rules.next_value') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="resetter.form.nextValue" name="nextValue" :max-length="18" inputmode="numeric" autocomplete="off" />
          </XhFieldControl>
          <XhFieldErrorText v-if="resetter.fieldError('nextValue')">
            {{ resetter.fieldError('nextValue') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot :invalid="!!resetter.fieldError('reason')" required>
          <XhFieldLabel>{{ t('settings.numbering_rules.reset_reason') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="resetter.form.reason" name="reason" type="textarea" :rows="2" :max-length="500" />
          </XhFieldControl>
          <XhFieldErrorText v-if="resetter.fieldError('reason')">
            {{ resetter.fieldError('reason') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <XhFieldRoot v-if="resetter.needsConfirm.value" :invalid="!!resetter.fieldError('confirmCode')" required>
          <XhFieldLabel>{{ t('settings.numbering_rules.confirm_rule_code') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="resetter.form.confirmCode" name="confirmCode" autocomplete="off" :placeholder="resetter.target.value?.code" />
          </XhFieldControl>
          <XhFieldErrorText v-if="resetter.fieldError('confirmCode')">
            {{ resetter.fieldError('confirmCode') }}
          </XhFieldErrorText>
        </XhFieldRoot>
        <p v-if="resetter.submitError.value" class="text-caption text-fg-danger" role="alert">
          {{ resetter.submitError.value }}
        </p>
      </XhFormRoot>
    </XEditModal>
  </SchemaPage>
</template>
