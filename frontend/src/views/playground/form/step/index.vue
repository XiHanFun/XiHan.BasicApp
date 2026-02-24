<script setup lang="ts">
import { ref } from 'vue'
import { NButton, NCard, NForm, NFormItem, NInput, NSpace, NSteps, NStep, useMessage } from 'naive-ui'

const message = useMessage()
const current = ref(1)
const form = ref({ account: '', password: '', confirm: '' })
function handleNext() {
  if (current.value < 3) current.value++
  else message.success('分步表单完成')
}
function handlePrev() {
  if (current.value > 1) current.value--
}
</script>

<template>
  <NCard title="分步表单" :bordered="false">
    <NSteps :current="current" class="mb-6" style="max-width: 600px">
      <NStep title="账户信息" />
      <NStep title="安全设置" />
      <NStep title="完成" />
    </NSteps>
    <NForm :model="form" label-placement="left" label-width="80" style="max-width: 500px">
      <template v-if="current === 1">
        <NFormItem label="账户">
          <NInput v-model:value="form.account" placeholder="请输入账户名" />
        </NFormItem>
      </template>
      <template v-else-if="current === 2">
        <NFormItem label="密码">
          <NInput v-model:value="form.password" type="password" placeholder="请输入密码" />
        </NFormItem>
        <NFormItem label="确认密码">
          <NInput v-model:value="form.confirm" type="password" placeholder="再次输入密码" />
        </NFormItem>
      </template>
      <template v-else>
        <p class="text-center text-lg text-foreground">设置完成</p>
      </template>
      <NSpace justify="end" class="mt-4">
        <NButton v-if="current > 1" @click="handlePrev">上一步</NButton>
        <NButton type="primary" @click="handleNext">{{ current < 3 ? '下一步' : '完成' }}</NButton>
      </NSpace>
    </NForm>
  </NCard>
</template>
