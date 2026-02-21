<script setup lang="ts">
import { h, onMounted, ref } from 'vue'
import { NCard, NDataTable, NTag, useMessage } from 'naive-ui'
import type { DataTableColumns } from 'naive-ui'
import type { SysPermission } from '~/types'
import { getPermissionListApi } from '@/api'

const message = useMessage()
const loading = ref(false)
const rows = ref<SysPermission[]>([])

const columns: DataTableColumns<SysPermission> = [
  { title: '权限名称', key: 'permissionName', width: 220 },
  {
    title: '权限编码',
    key: 'permissionCode',
    render: (row) => h(NTag, { type: 'info', bordered: false }, { default: () => row.permissionCode }),
  },
  { title: '分组', key: 'groupName', width: 180 },
  { title: '描述', key: 'description' },
]

async function fetchData() {
  loading.value = true
  try {
    rows.value = await getPermissionListApi()
  } catch {
    message.error('获取权限列表失败')
  } finally {
    loading.value = false
  }
}

onMounted(fetchData)
</script>

<template>
  <NCard title="权限管理" :bordered="false">
    <NDataTable :columns="columns" :data="rows" :loading="loading" :pagination="{ pageSize: 20 }" />
  </NCard>
</template>
