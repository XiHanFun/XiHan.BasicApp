<!--
  业务编号发号记录抽屉。
  职责：按规则远程分页、对关键词输入防抖，展示每次发号的首尾编号与业务关联；记录只读，不允许修改或删除。
-->
<script setup lang="ts">
import type { AllocationRow, RuleRow } from '../use-numbering-rules'
import type { XDataTableColumn } from '~/components'
import { XhDrawerCloseTrigger, XhDrawerContent, XhDrawerRoot, XhDrawerTitle, XhFlex } from '@xihan-ui/vue'
import { computed, h, onUnmounted, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { XDataTable, XInput } from '~/components'
import { toast } from '~/composables'
import { formatDate } from '~/utils'
import { useNumberingAllocations } from '../use-numbering-rules'

defineOptions({ name: 'NumberingAllocationDrawer' })

const props = defineProps<{
  show: boolean
  rule: null | RuleRow
}>()

const emit = defineEmits<{
  'update:show': [value: boolean]
}>()

const SEARCH_DEBOUNCE_MS = 300
const { t } = useI18n()
const allocations = useNumberingAllocations()
let searchTimer: ReturnType<typeof setTimeout> | undefined

const title = computed(() => t('settings.numbering_rules.allocation_title', { code: props.rule?.code ?? '' }))

const columns = computed<XDataTableColumn<AllocationRow>[]>(() => [
  { key: 'generatedAt', title: t('settings.numbering_rules.allocation_fields.generated_at'), width: 170, render: row => formatDate(row.generatedAt) },
  { key: 'requestTenantId', title: t('settings.numbering_rules.allocation_fields.requester'), width: 110 },
  { key: 'idempotencyKey', title: t('settings.numbering_rules.allocation_fields.idempotency_key'), minWidth: 180, ellipsis: true },
  { key: 'periodKey', title: t('settings.numbering_rules.allocation_fields.period'), width: 110 },
  { key: 'range', title: t('settings.numbering_rules.allocation_fields.serial_range'), width: 130, render: row => `${row.startValue} - ${row.endValue}` },
  { key: 'count', title: t('settings.numbering_rules.allocation_fields.count'), width: 80 },
  { key: 'firstNumber', title: t('settings.numbering_rules.allocation_fields.first_number'), minWidth: 170, ellipsis: true },
  { key: 'lastNumber', title: t('settings.numbering_rules.allocation_fields.last_number'), minWidth: 170, ellipsis: true },
  { key: 'business', title: t('settings.numbering_rules.allocation_fields.business'), minWidth: 160, render: row => h('span', `${row.businessType ?? '-'} / ${row.businessId ?? '-'}`) },
])

function reportLoadFailure(error: unknown) {
  toast.danger((error as Error).message || t('common.messages.load_failed'))
}

watch(
  () => [props.show, props.rule?.id] as const,
  ([show]) => {
    if (!show || !props.rule)
      return
    if (searchTimer)
      clearTimeout(searchTimer)
    allocations.open(props.rule).catch(reportLoadFailure)
  },
)

watch(allocations.keyword, () => {
  if (!props.show)
    return
  if (searchTimer)
    clearTimeout(searchTimer)
  searchTimer = setTimeout(() => {
    allocations.search().catch(reportLoadFailure)
  }, SEARCH_DEBOUNCE_MS)
})

onUnmounted(() => {
  if (searchTimer)
    clearTimeout(searchTimer)
  // 作废版本使组件卸载后返回的旧请求不再写入响应状态
  allocations.dispose()
})

function changePage(page: number): void {
  allocations.changePage(page).catch(reportLoadFailure)
}
</script>

<template>
  <XhDrawerRoot
    :open="show"
    side="right"
    @update:open="(open: boolean) => emit('update:show', open)"
  >
    <XhDrawerContent style="--xh-drawer-size: 1080px">
      <XhDrawerTitle>{{ title }}</XhDrawerTitle>
      <XhDrawerCloseTrigger />
      <XhFlex orientation="vertical" gap="md">
        <XInput
          v-model:value="allocations.keyword.value"
          clearable
          :placeholder="t('settings.numbering_rules.allocation_search_placeholder')"
          style="max-width: 360px"
        />
        <XDataTable
          :columns="columns"
          :data="allocations.items.value"
          :loading="allocations.loading.value"
          row-key="id"
          :pagination="{
            page: allocations.page.value,
            pageSize: allocations.pageSize,
            itemCount: allocations.total.value,
            onUpdatePage: changePage,
          }"
        />
      </XhFlex>
    </XhDrawerContent>
  </XhDrawerRoot>
</template>
