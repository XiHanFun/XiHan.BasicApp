<script setup lang="ts">
import { ref } from 'vue'
import { NButton, NCard, NForm, NFormItem, NInput, NInputNumber, NSpace, useMessage } from 'naive-ui'

const message = useMessage()
const formData = ref({
  name: '',
  quantity: 1,
  remark: '',
})

function handleSubmit() {
  message.success(`提交成功：${formData.value.name || '未命名'}`)
}
</script>

<template>
  <NCard title="表单示例" :bordered="false">
    <NForm :model="formData" label-placement="left" label-width="90">
      <NFormItem label="名称">
        <NInput v-model:value="formData.name" placeholder="请输入名称" />
      </NFormItem>
      <NFormItem label="数量">
        <NInputNumber v-model:value="formData.quantity" :min="1" />
      </NFormItem>
      <NFormItem label="备注">
        <NInput v-model:value="formData.remark" type="textarea" />
      </NFormItem>
      <NSpace justify="end">
        <NButton type="primary" @click="handleSubmit">提交</NButton>
      </NSpace>
    </NForm>
  </NCard>
</template>
