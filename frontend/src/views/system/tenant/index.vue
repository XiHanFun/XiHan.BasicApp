<script setup lang="ts">
import { h, onMounted, reactive, ref } from 'vue'
import { NButton, NCard, NDataTable, NInput, NPagination, NSpace, NTag, useMessage } from 'naive-ui'
import type { DataTableColumns } from 'naive-ui'
import type { SysTenant } from '~/types'
import { getTenantPageApi } from '@/api'

const message = useMessage()
const loading = ref(false)
const rows = ref<SysTenant[]>([])
const total = ref(0)
const query = reactive({ page: 1, pageSize: 20, keyword: '' })

const columns: DataTableColumns<SysTenant> = [
  { title: '租户名称', key: 'tenantName', width: 220 },
  { title: '租户编码', key: 'tenantCode', width: 180 },
  { title: '联系人', key: 'contactName', width: 150 },
  { title: '联系电话', key: 'contactPhone', width: 160 },
  {
    title: '状态',
    key: 'status',
    width: 100,
    render: (row) => h(NTag, { type: row.status === 1 ? 'success' : 'error' }, { default: () => (row.status === 1 ? '启用' : '禁用') }),
  },
  { title: '到期时间', key: 'expiredTime', width: 180 },
]

async function fetchData() {
  loading.value = true
  try {
    const data = await getTenantPageApi(query)
    rows.value = data.items
    total.value = data.total
  } catch {
    message.error('获取租户列表失败')
  } finally {
    loading.value = false
  }
}

onMounted(fetchData)
</script>

<template>
  <div class="space-y-4">
    <NCard :bordered="false">
      <NSpace>
        <NInput v-model:value="query.keyword" placeholder="搜索租户名称/编码" clearable @keydown.enter="fetchData" />
        <NButton type="primary" @click="fetchData">搜索</NButton>
      </NSpace>
    </NCard>
    <NCard title="租户管理" :bordered="false">
      <NDataTable :columns="columns" :data="rows" :loading="loading" :pagination="false" :row-key="(row) => row.id" />
      <div class="mt-4 flex justify-end">
        <NPagination
          v-model:page="query.page"
          v-model:page-size="query.pageSize"
          :item-count="total"
          @update:page="fetchData"
          @update:page-size="() => { query.page = 1; fetchData() }"
        />
      </div>
    </NCard>
  </div>
</template>
