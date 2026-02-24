<script setup lang="ts">
import { NCard, NDataTable } from 'naive-ui'

const columns = [
  { title: 'ID', key: 'id', width: 80 },
  { title: '姓名', key: 'name' },
  { title: '年龄', key: 'age', width: 80 },
  { title: '邮箱', key: 'email' },
]
const data = Array.from({ length: 8 }, (_, i) => ({
  id: i + 1,
  name: `用户${i + 1}`,
  age: 20 + (i % 30),
  email: `user${i + 1}@example.com`,
}))
</script>

<template>
  <NCard title="基础表格" :bordered="false">
    <NDataTable :columns="columns" :data="data" :bordered="false" />
  </NCard>
</template>
