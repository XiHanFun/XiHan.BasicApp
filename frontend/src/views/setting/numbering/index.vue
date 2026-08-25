<!--
  系统设置 / 业务编号页面。
  职责：按平台或租户上下文展示规则、只读全局规则、规则管理、安全重置和永久发号记录；页面不提供真实发号入口。
-->
<script setup lang="ts">
import type {
  NumberingRuleDetailDto,
  NumberingRuleListItemDto,
  PageResult,
} from '@/api'
import type { ListFieldSchema, PageSchema, SchemaActionPayload } from '~/components'
import type { EnumOptionItem } from '~/hooks'
import { XhAlertDescription, XhAlertIcon, XhAlertRoot, XhButton, XhDescriptionsItem, XhDescriptionsLabel, XhDescriptionsRoot, XhDescriptionsValue, XhDialogCloseTrigger, XhDialogContent, XhDialogRoot, XhDialogTitle, XhFieldControl, XhFieldErrorText, XhFieldLabel, XhFieldRoot, XhFormRoot, XhSpinner, XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { computed, h, onMounted, ref, useId, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import {
  createPageRequest,
  EnableStatus,
  numberingApi,
  NumberingDateFormat,
  NumberingResetCycle,
  NumberingScope,
  querySortsFromSchema,
  tenantApi,
} from '@/api'
import { SchemaPage, XEditModal, XInput, XTooltip } from '~/components'
import { dialog, toast } from '~/composables'
import { useEnumOptions } from '~/hooks'
import { Icon } from '~/iconify'
import { useUserStore } from '~/stores'
import NumberingAllocationDrawer from './components/NumberingAllocationDrawer.vue'
import NumberingPreviewModal from './components/NumberingPreviewModal.vue'
import NumberingRuleEditor from './components/NumberingRuleEditor.vue'

defineOptions({ name: 'SettingNumberingPage' })

const { t } = useI18n()

/** 编辑弹窗的保存钮靠这个 id 关联到表单，点它才会走整表校验 */
const editFormId = useId()
const userStore = useUserStore()
const schemaPageRef = ref<InstanceType<typeof SchemaPage> | null>(null)
const isPlatform = ref(!userStore.userInfo?.tenantId)
const contextResolved = ref(false)
const activeScope = ref<NumberingScope>(isPlatform.value ? NumberingScope.Global : NumberingScope.Tenant)
const actionLoading = ref(false)

const canMaintain = computed(() => activeScope.value === NumberingScope.Tenant
  || (isPlatform.value && userStore.hasPermission('saas:numbering:global-manage')))

/** 当前租户视图的作用域名称，用于让紧凑工具栏按钮仍具备明确的无障碍语义。 */
const currentScopeLabel = computed(() => activeScope.value === NumberingScope.Tenant
  ? t('setting.numbering.tenant_rules')
  : t('setting.numbering.available_global_rules'))

/** 点击切换后将进入的目标作用域名称。 */
const targetScopeLabel = computed(() => activeScope.value === NumberingScope.Tenant
  ? t('setting.numbering.available_global_rules')
  : t('setting.numbering.tenant_rules'))

/**
 * 生成作用域切换按钮的提示及无障碍名称。
 *
 * 按钮采用纯图标以节省纵向空间，因此必须同时描述当前作用域和目标作用域，避免仅凭图标猜测状态。
 */
const scopeSwitchLabel = computed(() => t('setting.numbering.switch_scope_tip', {
  current: currentScopeLabel.value,
  target: targetScopeLabel.value,
}))

watch(isPlatform, (platform) => {
  activeScope.value = platform ? NumberingScope.Global : NumberingScope.Tenant
})

/**
 * 在租户私有规则与租户可用的全局规则之间切换。
 *
 * 平台上下文只有全局规则这一种合法作用域，因此模板不会向平台管理员渲染该按钮；这里仍保留平台保护，
 * 防止未来通过快捷键或其他调用入口误把平台页面切换到租户作用域。
 */
function switchScope(): void {
  if (isPlatform.value)
    return

  activeScope.value = activeScope.value === NumberingScope.Tenant
    ? NumberingScope.Global
    : NumberingScope.Tenant
}

/**
 * 从租户切换列表解析当前运行上下文。
 *
 * 平台管理员切换租户后，历史 `isPlatform` 用户字段可能仍保留登录时状态；后端返回的
 * `TenantSwitcherDto.isCurrent` 按当前令牌实时计算，因此与控制中心共用它作为事实源。
 * 查询失败时回退到用户信息中的租户标识，权限校验仍由后端最终兜底。
 */
async function resolveCurrentContext(): Promise<void> {
  try {
    const tenants = await tenantApi.myAvailableTenants()
    isPlatform.value = !tenants.some(tenant => tenant.isCurrent)
  }
  catch {
    isPlatform.value = !userStore.userInfo?.tenantId
  }
  finally {
    contextResolved.value = true
  }
}

onMounted(() => {
  void resolveCurrentContext()
})

// 枚举标签一律取自后端枚举元数据，切语言时随之响应式刷新；这里只把选项列表转成按枚举值查找的映射。
const dateFormatOptions = useEnumOptions('NumberingDateFormat', [
  { label: 'yyyyMMdd', value: NumberingDateFormat.YyyyMMdd },
])
const resetCycleOptions = useEnumOptions('NumberingResetCycle', [
  { label: 'Daily', value: NumberingResetCycle.Daily },
])

// 枚举选项的 value 是成员名（见 app/context.ts 的枚举选项归一），与接口返回的字符串枚举一致，因此按字符串建索引。
function toLabelMap(options: EnumOptionItem[]): Record<string, string> {
  return Object.fromEntries(options.map(option => [String(option.value), option.label]))
}

const dateFormatLabels = computed(() => toLabelMap(dateFormatOptions.value))
const resetCycleLabels = computed(() => toLabelMap(resetCycleOptions.value))

const fields = computed<ListFieldSchema[]>(() => [
  { key: 'keyword', title: t('setting.numbering.keyword'), dataType: 'string', visible: false, searchable: true, searchPlaceholder: t('setting.numbering.keyword_placeholder'), order: 0 },
  { key: 'ruleCode', title: t('setting.numbering.rule_code'), dataType: 'string', sortable: true, minWidth: 150, order: 1 },
  { key: 'ruleName', title: t('setting.numbering.rule_name'), dataType: 'string', sortable: true, minWidth: 140, order: 2 },
  {
    key: 'format',
    title: t('setting.numbering.format'),
    dataType: 'string',
    minWidth: 210,
    order: 3,
    render: row => formatTemplate(row as unknown as NumberingRuleListItemDto),
  },
  { key: 'resetCycle', title: t('setting.numbering.reset_cycle'), dataType: 'enum', width: 100, order: 4, render: row => resetCycleLabels.value[(row as unknown as NumberingRuleListItemDto).resetCycle] },
  { key: 'timeZoneId', title: t('setting.numbering.time_zone'), dataType: 'string', width: 140, order: 5 },
  { key: 'currentPeriod', title: t('setting.numbering.period'), dataType: 'string', width: 110, order: 6, render: row => (row as unknown as NumberingRuleListItemDto).currentPeriod || '-' },
  { key: 'currentValue', title: t('setting.numbering.current_value'), dataType: 'string', width: 120, order: 7 },
  {
    key: 'allowTenantUse',
    title: t('setting.numbering.allow_tenant_use'),
    dataType: 'boolean',
    width: 110,
    order: 8,
    visible: activeScope.value === NumberingScope.Global,
    render: row => h(XhTagRoot, { variant: 'subtle', tone: (row as unknown as NumberingRuleListItemDto).allowTenantUse ? 'success' : 'neutral', size: 'sm' }, () => h(XhTagLabel, () => (row as unknown as NumberingRuleListItemDto).allowTenantUse ? t('common.statuses.yes') : t('common.statuses.no'))),
  },
  {
    key: 'status',
    title: t('setting.numbering.status'),
    dataType: 'enum',
    width: 90,
    sortable: true,
    order: 9,
    dictionaryCode: 'EnableStatus',
    render: row => h(XhTagRoot, { variant: 'subtle', tone: (row as unknown as NumberingRuleListItemDto).status === EnableStatus.Enabled ? 'success' : 'danger', size: 'sm' }, () => h(XhTagLabel, () => (row as unknown as NumberingRuleListItemDto).status === EnableStatus.Enabled ? t('common.statuses.enabled') : t('common.statuses.disabled'))),
  },
])

const schema = computed<PageSchema>(() => ({
  pageCode: 'setting.numbering',
  pageName: t('setting.numbering.page_name'),
  rowKey: 'basicId',
  fields: fields.value,
  resource: {
    page: (params) => {
      const input = {
        ...createPageRequest({
          page: { pageIndex: params.page, pageSize: params.pageSize },
          conditions: { sorts: querySortsFromSchema(params.sorts), filters: params.conditionFilters ?? [] },
        }),
        scope: activeScope.value,
        keyword: String(params.filters.keyword ?? '').trim() || undefined,
      }
      const promise = !isPlatform.value && activeScope.value === NumberingScope.Global
        ? numberingApi.availableGlobalPage(input)
        : numberingApi.page(input)
      return promise as unknown as Promise<PageResult<Record<string, unknown>>>
    },
  },
  actions: [
    // SchemaActionPanel 仅按权限筛选页面级动作，因此只在可维护作用域中组装“新增”按钮。
    ...(canMaintain.value
      ? [{ key: 'create', title: t('setting.numbering.action_create'), scope: 'page' as const, type: 'primary' as const, icon: 'lucide:plus', permission: 'saas:numbering:create' }]
      : []),
    { key: 'preview', title: t('setting.numbering.action_preview'), scope: 'row', icon: 'lucide:scan-eye', permission: 'saas:numbering:read' },
    { key: 'view', title: t('setting.numbering.action_view'), scope: 'row', icon: 'lucide:eye' },
    { key: 'edit', title: t('setting.numbering.action_edit'), scope: 'row', icon: 'lucide:pen', permission: 'saas:numbering:update', visible: () => canMaintain.value },
    { key: 'toggle', title: t('setting.numbering.action_toggle'), scope: 'row', icon: 'lucide:power', permission: 'saas:numbering:status', visible: () => canMaintain.value },
    { key: 'reset', title: t('setting.numbering.action_reset'), scope: 'row', icon: 'lucide:rotate-ccw', permission: 'saas:numbering:reset', visible: () => canMaintain.value },
    { key: 'allocations', title: t('setting.numbering.action_allocations'), scope: 'row', icon: 'lucide:history', permission: 'saas:numbering:allocation-read' },
    { key: 'delete', title: t('setting.numbering.action_delete'), scope: 'row', type: 'error', icon: 'lucide:trash-2', permission: 'saas:numbering:delete', visible: () => canMaintain.value, disabled: row => (row as unknown as NumberingRuleListItemDto).hasAllocated },
  ],
}))

/** 将规则字段渲染为不消耗流水的格式模板文本。 */
function formatTemplate(rule: NumberingRuleListItemDto): string {
  const dateSegment = rule.dateFormat === NumberingDateFormat.None ? null : dateFormatLabels.value[rule.dateFormat]
  const segments = [rule.prefix, dateSegment, '0'.repeat(Math.min(rule.serialLength, 18))]
  return segments.filter(Boolean).join(rule.separator)
}

const editorVisible = ref(false)
const editorDetail = ref<NumberingRuleDetailDto | null>(null)
const detailVisible = ref(false)
const detail = ref<NumberingRuleDetailDto | null>(null)
const allocationVisible = ref(false)
const selectedRule = ref<NumberingRuleListItemDto | null>(null)
const previewVisible = ref(false)
const previewRule = ref<NumberingRuleListItemDto | null>(null)
const resetVisible = ref(false)
const resetTarget = ref<NumberingRuleListItemDto | null>(null)
const resetForm = ref({ nextValue: '1', reason: '', confirmRuleCode: '' })

/** 分发 SchemaPage 页面与行操作。 */
function onAction(payload: SchemaActionPayload): void {
  const row = payload.row as unknown as NumberingRuleListItemDto | undefined
  if (payload.key === 'create') {
    editorDetail.value = null
    editorVisible.value = true
    return
  }

  if (!row)
    return

  switch (payload.key) {
    case 'preview':
      previewRule.value = row
      previewVisible.value = true
      break
    case 'view':
      void openDetail(row)
      break
    case 'edit':
      void openEditor(row)
      break
    case 'toggle':
      void toggleStatus(row)
      break
    case 'reset':
      openReset(row)
      break
    case 'allocations':
      selectedRule.value = row
      allocationVisible.value = true
      break
    case 'delete':
      remove(row)
      break
  }
}

/** 加载并打开只读详情。 */
async function openDetail(row: NumberingRuleListItemDto): Promise<void> {
  try {
    detail.value = await numberingApi.detail(row.basicId, activeScope.value)
    if (!detail.value) {
      toast.error(t('setting.numbering.not_found'))
      return
    }
    detailVisible.value = true
  }
  catch (error) {
    toast.error((error as Error).message || t('setting.numbering.not_found'))
  }
}

/** 加载并打开编辑器。 */
async function openEditor(row: NumberingRuleListItemDto): Promise<void> {
  try {
    editorDetail.value = await numberingApi.detail(row.basicId, activeScope.value)
    if (!editorDetail.value) {
      toast.error(t('setting.numbering.not_found'))
      return
    }
    editorVisible.value = true
  }
  catch (error) {
    toast.error((error as Error).message || t('setting.numbering.not_found'))
  }
}

/** 启停规则；全局 actionLoading 锁阻止快速重复点击产生并发写。 */
async function toggleStatus(row: NumberingRuleListItemDto): Promise<void> {
  if (actionLoading.value)
    return
  actionLoading.value = true
  try {
    await numberingApi.updateStatus({ basicId: row.basicId, scope: activeScope.value, status: row.status === EnableStatus.Enabled ? EnableStatus.Disabled : EnableStatus.Enabled })
    toast.success(t('setting.numbering.status_success'))
    void schemaPageRef.value?.reload()
  }
  catch (error) {
    toast.error((error as Error).message || t('setting.numbering.status_failed'))
  }
  finally {
    actionLoading.value = false
  }
}

/** 初始化安全重置弹窗。 */
function openReset(row: NumberingRuleListItemDto): void {
  resetTarget.value = row
  resetForm.value = { nextValue: (BigInt(row.currentValue) + 1n).toString(), reason: '', confirmRuleCode: '' }
  resetVisible.value = true
}

/** 提交安全重置；18 位流水按字符串传输以避免 JavaScript 精度丢失。 */
async function submitReset(): Promise<void> {
  if (!resetTarget.value || actionLoading.value)
    return
  if (!/^\d{1,18}$/.test(resetForm.value.nextValue) || BigInt(resetForm.value.nextValue) < 1n || !resetForm.value.reason.trim()) {
    toast.warning(t('setting.numbering.reset_required'))
    return
  }
  actionLoading.value = true
  try {
    await numberingApi.reset({
      basicId: resetTarget.value.basicId,
      scope: activeScope.value,
      nextValue: resetForm.value.nextValue,
      reason: resetForm.value.reason.trim(),
      confirmRuleCode: resetTarget.value.isGlobal ? resetForm.value.confirmRuleCode.trim() : null,
    })
    toast.success(t('setting.numbering.reset_success'))
    resetVisible.value = false
    void schemaPageRef.value?.reload()
  }
  catch (error) {
    toast.error((error as Error).message || t('setting.numbering.reset_failed'))
  }
  finally {
    actionLoading.value = false
  }
}

/** 确认删除从未发号的规则。 */
function remove(row: NumberingRuleListItemDto): void {
  void dialog.confirm({
    badge: 'warning',
    tone: 'danger',
    title: t('setting.numbering.delete_title'),
    content: t('setting.numbering.delete_confirm', { code: row.ruleCode }),
    okText: t('common.actions.delete'),
    cancelText: t('common.actions.cancel'),
    onOk: async () => {
      if (actionLoading.value)
        return false
      actionLoading.value = true
      try {
        await numberingApi.delete(row.basicId, activeScope.value)
        toast.success(t('setting.numbering.delete_success'))
        void schemaPageRef.value?.reload()
        return true
      }
      catch (error) {
        toast.error((error as Error).message || t('setting.numbering.delete_failed'))
        return false
      }
      finally {
        actionLoading.value = false
      }
    },
  })
}
</script>

<template>
  <div class="flex h-full min-h-0 flex-col gap-3">
    <XhSpinner v-if="!contextResolved" class="flex-1 py-12" />
    <template v-else>
      <!--
        SchemaPage 内部表格依赖确定高度；flex-1 + min-h-0 将剩余视口高度正确传递给表格滚动区。
        activeScope 作为 key 会在切换时重建列表并自动加载一次，既重置上一作用域的筛选状态，也避免额外重复请求。
      -->
      <SchemaPage ref="schemaPageRef" :key="activeScope" class="min-h-0 flex-1" :schema="schema" @action="onAction">
        <template v-if="!isPlatform" #toolbar>
          <XTooltip :content="scopeSwitchLabel">
            <XhButton

              data-circle
              variant="ghost"
              size="sm"
              :aria-label="scopeSwitchLabel"
              @click="switchScope"
            >
              <!-- 图标表示点击后进入的目标作用域，Tooltip 同时补充当前状态和完整动作语义。 -->
              <span><Icon :icon="activeScope === NumberingScope.Tenant ? 'lucide:globe-2' : 'lucide:building-2'" /></span>
            </XhButton>
          </XTooltip>
        </template>
      </SchemaPage>
    </template>

    <NumberingRuleEditor
      v-model:show="editorVisible"
      :detail="editorDetail"
      :scope="activeScope"
      :global-mode="activeScope === NumberingScope.Global"
      @saved="schemaPageRef?.reload()"
    />
    <NumberingAllocationDrawer v-model:show="allocationVisible" :rule="selectedRule" :scope="activeScope" />
    <NumberingPreviewModal v-model:show="previewVisible" :rule="previewRule" />

    <XhDialogRoot v-model:open="detailVisible">
      <XhDialogContent style="--xh-dialog-max-w: 680px">
        <XhDialogTitle>{{ t('setting.numbering.detail_title') }}</XhDialogTitle>
        <XhDialogCloseTrigger />
        <XhDescriptionsRoot v-if="detail" :columns="2" bordered placement="left">
          <XhDescriptionsItem>
            <XhDescriptionsLabel>{{ t('setting.numbering.rule_code') }}</XhDescriptionsLabel>
            <XhDescriptionsValue>
              {{ detail.ruleCode }}
            </XhDescriptionsValue>
          </XhDescriptionsItem>
          <XhDescriptionsItem>
            <XhDescriptionsLabel>{{ t('setting.numbering.rule_name') }}</XhDescriptionsLabel>
            <XhDescriptionsValue>
              {{ detail.ruleName }}
            </XhDescriptionsValue>
          </XhDescriptionsItem>
          <XhDescriptionsItem>
            <XhDescriptionsLabel>{{ t('setting.numbering.period') }}</XhDescriptionsLabel>
            <XhDescriptionsValue>
              {{ detail.currentPeriod || '-' }}
            </XhDescriptionsValue>
          </XhDescriptionsItem>
          <XhDescriptionsItem>
            <XhDescriptionsLabel>{{ t('setting.numbering.current_value') }}</XhDescriptionsLabel>
            <XhDescriptionsValue>
              {{ detail.currentValue }}
            </XhDescriptionsValue>
          </XhDescriptionsItem>
          <XhDescriptionsItem>
            <XhDescriptionsLabel>{{ t('setting.numbering.time_zone') }}</XhDescriptionsLabel>
            <XhDescriptionsValue>
              {{ detail.timeZoneId }}
            </XhDescriptionsValue>
          </XhDescriptionsItem>
          <XhDescriptionsItem>
            <XhDescriptionsLabel>{{ t('setting.numbering.has_allocated') }}</XhDescriptionsLabel>
            <XhDescriptionsValue>
              {{ detail.hasAllocated ? t('common.statuses.yes') : t('common.statuses.no') }}
            </XhDescriptionsValue>
          </XhDescriptionsItem>
          <XhDescriptionsItem style="grid-column: span 2">
            <XhDescriptionsLabel>{{ t('setting.numbering.remark') }}</XhDescriptionsLabel>
            <XhDescriptionsValue>
              {{ detail.remark || '-' }}
            </XhDescriptionsValue>
          </XhDescriptionsItem>
        </XhDescriptionsRoot>
      </XhDialogContent>
    </XhDialogRoot>

    <XEditModal v-model:show="resetVisible" :title="t('setting.numbering.reset_title')" :loading="actionLoading" :form-id="editFormId">
      <XhAlertRoot tone="warning" class="mb-3">
        <XhAlertIcon>
          <Icon icon="lucide:triangle-alert" width="16" />
        </XhAlertIcon>
        <XhAlertDescription>
          {{ t('setting.numbering.reset_tip') }}
        </XhAlertDescription>
      </XhAlertRoot>
      <XhFormRoot
        :id="editFormId"
        v-model:values="resetForm"
        validate-on="blur"
        @submit="submitReset"
      >
        <XhFieldRoot>
          <XhFieldLabel>{{ t('setting.numbering.next_value') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="resetForm.nextValue" :max-length="18" />
          </XhFieldControl>
          <XhFieldErrorText />
        </XhFieldRoot>
        <XhFieldRoot>
          <XhFieldLabel>{{ t('setting.numbering.reset_reason') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="resetForm.reason" type="textarea" :max-length="500" />
          </XhFieldControl>
          <XhFieldErrorText />
        </XhFieldRoot>
        <XhFieldRoot v-if="resetTarget?.isGlobal">
          <XhFieldLabel>{{ t('setting.numbering.confirm_rule_code') }}</XhFieldLabel>
          <XhFieldControl>
            <XInput v-model:value="resetForm.confirmRuleCode" :placeholder="resetTarget.ruleCode" />
          </XhFieldControl>
          <XhFieldErrorText />
        </XhFieldRoot>
      </XhFormRoot>
    </XEditModal>
  </div>
</template>
