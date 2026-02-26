<script setup lang="ts">
import { computed, h, onMounted, reactive, ref } from 'vue'
import { NCard, NDataTable, NPagination, NRadioButton, NRadioGroup } from 'naive-ui'
import type { DataTableColumns } from 'naive-ui'
import type { SysLogItem } from '~/types'
import { formatDate } from '~/utils'
import { getAccessLogsApi, getAuditLogsApi, getExceptionLogsApi, getOperationLogsApi } from '~/api'

const loading = ref(false)
const rows = ref<SysLogItem[]>([])
const total = ref(0)
const query = reactive({ page: 1, pageSize: 20 })
const activeType = ref<'access' | 'operation' | 'exception' | 'audit'>('audit')

const columns: DataTableColumns<SysLogItem> = [
  { title: '用户', key: 'userName', width: 140 },
  { title: '操作', key: 'operationName', width: 180 },
  { title: 'IP', key: 'ip', width: 140 },
  { title: '消息', key: 'message' },
  {
    title: '时间',
    key: 'createdTime',
    width: 180,
    render: (row) => h('span', null, formatDate(row.createdTime ?? '')),
  },
]

const loaders = computed(() => ({
  access: getAccessLogsApi,
  operation: getOperationLogsApi,
  exception: getExceptionLogsApi,
  audit: getAuditLogsApi,
}))

async function fetchData() {
  loading.value = true
  try {
    const data = await loaders.value[activeType.value](query)
    rows.value = data.items
    total.value = data.total
  } finally {
    loading.value = false
  }
}

onMounted(fetchData)
</script>

<template>
  <NCard title="日志中心" :bordered="false">
    <NRadioGroup v-model:value="activeType" class="mb-4" @update:value="fetchData">
      <NRadioButton value="audit">审计日志</NRadioButton>
      <NRadioButton value="operation">操作日志</NRadioButton>
      <NRadioButton value="access">访问日志</NRadioButton>
      <NRadioButton value="exception">异常日志</NRadioButton>
    </NRadioGroup>
    <NDataTable :columns="columns" :data="rows" :loading="loading" :pagination="false" />
    <div class="mt-4 flex justify-end">
      <NPagination
        v-model:page="query.page"
        v-model:page-size="query.pageSize"
        :item-count="total"
        @update:page="fetchData"
        @update:page-size="
          () => {
            query.page = 1
            fetchData()
          }
        "
      />
    </div>
  </NCard>
</template>
