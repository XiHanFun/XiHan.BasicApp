<script setup lang="ts">
import { NCard, NDataTable } from 'naive-ui'

const columns = [
  { title: 'ID', key: 'id', width: 80 },
  { title: '名称', key: 'name', width: 200 },
  { title: '描述', key: 'desc' },
  { title: '数值', key: 'value', width: 120 },
]
const data = Array.from({ length: 5000 }, (_, i) => ({
  id: i + 1,
  name: `数据项 ${i + 1}`,
  desc: `这是第 ${i + 1} 条数据的详细描述信息`,
  value: Math.floor(Math.random() * 10000),
}))
</script>

<template>
  <NCard title="虚拟滚动表格（5000 行）" :bordered="false">
    <NDataTable
      :columns="columns"
      :data="data"
      :bordered="false"
      virtual-scroll
      :max-height="500"
    />
  </NCard>
</template>
