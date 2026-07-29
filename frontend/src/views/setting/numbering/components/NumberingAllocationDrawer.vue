<!--
  业务编号永久发号记录抽屉。
  职责：按规则远程分页、对关键词输入防抖，并展示格式快照重建的首尾编号；不允许修改或删除审计记录。
-->
<script setup lang="ts">
import type { DataTableColumns } from 'naive-ui'
import type {
  NumberingAllocationListItemDto,
  NumberingRuleListItemDto,
  NumberingScope,
} from '@/api'
import { NDataTable, NDrawer, NDrawerContent, NInput, NSpace, useMessage } from 'naive-ui'
import { computed, h, onUnmounted, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { createPageRequest, numberingApi } from '@/api'
import { formatDate } from '~/utils'

defineOptions({ name: 'NumberingAllocationDrawer' })

const props = defineProps<{
  show: boolean
  rule: NumberingRuleListItemDto | null
  scope: NumberingScope
}>()

const emit = defineEmits<{
  'update:show': [value: boolean]
}>()

const SearchDebounceMilliseconds = 300
const { t } = useI18n()
const message = useMessage()
const loading = ref(false)
const items = ref<NumberingAllocationListItemDto[]>([])
const keyword = ref('')
const pagination = ref({ page: 1, pageSize: 10, itemCount: 0 })
let searchTimer: ReturnType<typeof setTimeout> | undefined
let requestVersion = 0

const title = computed(() => t('setting.numbering.allocation_title', { code: props.rule?.ruleCode ?? '' }))

const columns = computed<DataTableColumns<NumberingAllocationListItemDto>>(() => [
  { key: 'generatedAtUtc', title: t('setting.numbering.generated_at'), width: 170, render: row => formatDate(row.generatedAtUtc) },
  { key: 'requestTenantId', title: t('setting.numbering.request_tenant'), width: 110 },
  { key: 'idempotencyKey', title: t('setting.numbering.idempotency_key'), minWidth: 180, ellipsis: { tooltip: true } },
  { key: 'periodKey', title: t('setting.numbering.period'), width: 110 },
  { key: 'range', title: t('setting.numbering.serial_range'), width: 130, render: row => `${row.startValue} - ${row.endValue}` },
  { key: 'count', title: t('setting.numbering.count'), width: 80 },
  { key: 'firstNumber', title: t('setting.numbering.first_number'), minWidth: 170, ellipsis: { tooltip: true } },
  { key: 'lastNumber', title: t('setting.numbering.last_number'), minWidth: 170, ellipsis: { tooltip: true } },
  {
    key: 'business',
    title: t('setting.numbering.business'),
    minWidth: 160,
    render: row => h('span', `${row.businessType ?? '-'} / ${row.businessId ?? '-'}`),
  },
])

watch(
  () => [props.show, props.rule?.basicId, props.scope] as const,
  ([show]) => {
    if (!show || !props.rule)
      return
    keyword.value = ''
    pagination.value.page = 1
    void load()
  },
)

watch(keyword, () => {
  if (!props.show)
    return
  if (searchTimer)
    clearTimeout(searchTimer)
  searchTimer = setTimeout(() => {
    pagination.value.page = 1
    void load()
  }, SearchDebounceMilliseconds)
})

onUnmounted(() => {
  if (searchTimer)
    clearTimeout(searchTimer)
  // 递增版本使组件卸载后返回的旧请求不再写入响应状态。
  requestVersion++
})

/**
 * 加载当前规则发号记录；请求版本避免快速切换规则时旧响应覆盖新结果。
 * @returns 完成信号。
 * @throws 网络错误由统一请求层包装后展示。
 */
async function load(): Promise<void> {
  if (!props.rule)
    return
  const version = ++requestVersion
  loading.value = true
  try {
    const result = await numberingApi.allocationPage({
      ...createPageRequest({
        page: { pageIndex: pagination.value.page, pageSize: pagination.value.pageSize },
      }),
      ruleId: props.rule.basicId,
      scope: props.scope,
      keyword: keyword.value.trim() || undefined,
    })
    if (version !== requestVersion)
      return
    items.value = result.items
    pagination.value.itemCount = result.page.totalCount
  }
  catch (error) {
    if (version === requestVersion)
      message.error((error as Error).message || t('setting.numbering.allocation_load_failed'))
  }
  finally {
    if (version === requestVersion)
      loading.value = false
  }
}

/**
 * 切换远程分页页码。
 * @param page 新页码。
 * @returns 无返回值。
 */
function changePage(page: number): void {
  pagination.value.page = page
  void load()
}
</script>

<template>
  <NDrawer :show="show" :width="1080" @update:show="emit('update:show', $event)">
    <NDrawerContent closable :title="title">
      <NSpace vertical :size="12">
        <NInput
          v-model:value="keyword"
          clearable
          :placeholder="t('setting.numbering.allocation_search_placeholder')"
          style="max-width: 360px"
        />
        <NDataTable
          remote
          :columns="columns"
          :data="items"
          :loading="loading"
          :row-key="(row: NumberingAllocationListItemDto) => row.basicId"
          :scroll-x="1450"
          :pagination="{
            page: pagination.page,
            pageSize: pagination.pageSize,
            itemCount: pagination.itemCount,
            onUpdatePage: changePage,
          }"
        />
      </NSpace>
    </NDrawerContent>
  </NDrawer>
</template>
