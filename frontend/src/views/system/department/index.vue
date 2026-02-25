<script setup lang="ts">
import type { DataTableColumns } from 'naive-ui'
import type { SysDepartment } from '~/types'
import { NCard, NDataTable, NTag, useMessage } from 'naive-ui'
import { h, onMounted, ref } from 'vue'
import { getDepartmentTreeApi } from '~/api'

const message = useMessage()
const loading = ref(false)
const rows = ref<SysDepartment[]>([])

const columns: DataTableColumns<SysDepartment> = [
  { title: '部门名称', key: 'departmentName', width: 220 },
  { title: '部门编码', key: 'departmentCode', width: 180 },
  { title: '负责人', key: 'leader', width: 160 },
  { title: '联系电话', key: 'phone', width: 160 },
  {
    title: '状态',
    key: 'status',
    width: 100,
    render: (row) =>
      h(
        NTag,
        { type: row.status === 1 ? 'success' : 'warning', round: true },
        { default: () => (row.status === 1 ? '启用' : '停用') },
      ),
  },
]

async function fetchData() {
  loading.value = true
  try {
    rows.value = await getDepartmentTreeApi()
  } catch {
    message.error('获取部门树失败')
  } finally {
    loading.value = false
  }
}

onMounted(fetchData)
</script>

<template>
  <NCard title="部门管理" :bordered="false">
    <NDataTable
      :columns="columns"
      :data="rows"
      :loading="loading"
      :pagination="false"
      :default-expand-all="true"
      :row-key="(row) => row.id"
    />
  </NCard>
</template>
