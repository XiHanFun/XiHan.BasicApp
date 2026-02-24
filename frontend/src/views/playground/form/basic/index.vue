<script setup lang="ts">
import { ref } from 'vue'
import { NButton, NCard, NForm, NFormItem, NInput, NSelect, NSpace, useMessage } from 'naive-ui'

const message = useMessage()
const form = ref({ name: '', email: '', type: null as string | null })
const typeOptions = [
  { label: '个人', value: 'personal' },
  { label: '企业', value: 'enterprise' },
]
function handleSubmit() {
  message.success('提交成功')
}
</script>

<template>
  <NCard title="基础表单" :bordered="false">
    <NForm :model="form" label-placement="left" label-width="80" style="max-width: 500px">
      <NFormItem label="姓名">
        <NInput v-model:value="form.name" placeholder="请输入姓名" />
      </NFormItem>
      <NFormItem label="邮箱">
        <NInput v-model:value="form.email" placeholder="请输入邮箱" />
      </NFormItem>
      <NFormItem label="类型">
        <NSelect v-model:value="form.type" :options="typeOptions" placeholder="请选择" />
      </NFormItem>
      <NSpace justify="end">
        <NButton type="primary" @click="handleSubmit">提交</NButton>
      </NSpace>
    </NForm>
  </NCard>
</template>
