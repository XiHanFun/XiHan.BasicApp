<script setup lang="ts">
import { NCard, NDataTable } from 'naive-ui'

const columns = [
  { title: 'ID', key: 'id', width: 80 },
  { title: '名称', key: 'name' },
  { title: '描述', key: 'desc' },
]

const rows = Array.from({ length: 8 }, (_, i) => ({
  id: i + 1,
  name: `示例项-${i + 1}`,
  desc: '用于验证通用表格能力（分页/列定义/滚动）',
}))
</script>

<template>
  <NCard title="表格示例" :bordered="false">
    <NDataTable :columns="columns" :data="rows" :pagination="{ pageSize: 5 }" />
  </NCard>
</template>
