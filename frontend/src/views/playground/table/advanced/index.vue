<script setup lang="ts">
import { ref } from 'vue'
import { NCard, NDataTable, NInput, NSpace } from 'naive-ui'

const keyword = ref('')
const columns = [
  { title: 'ID', key: 'id', width: 80, sorter: (a: any, b: any) => a.id - b.id },
  { title: '姓名', key: 'name', filter: true },
  { title: '部门', key: 'dept' },
  { title: '职位', key: 'role' },
  { title: '状态', key: 'status' },
]
const allData = Array.from({ length: 20 }, (_, i) => ({
  id: i + 1,
  name: `员工${i + 1}`,
  dept: ['研发部', '产品部', '设计部', '运营部'][i % 4],
  role: ['前端工程师', '后端工程师', '产品经理', 'UI 设计师'][i % 4],
  status: i % 3 === 0 ? '休假' : '在岗',
}))
</script>

<template>
  <NCard title="高级表格" :bordered="false">
    <NSpace vertical>
      <NInput v-model:value="keyword" placeholder="搜索姓名..." clearable style="max-width: 300px" />
      <NDataTable
        :columns="columns"
        :data="keyword ? allData.filter(r => r.name.includes(keyword)) : allData"
        :bordered="false"
        :pagination="{ pageSize: 10 }"
      />
    </NSpace>
  </NCard>
</template>
