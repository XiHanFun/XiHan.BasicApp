<script setup lang="ts">
import { ref } from 'vue'
import { NButton, NCard, NForm, NFormItem, NInput, NSpace, useMessage } from 'naive-ui'

const message = useMessage()
const items = ref([{ key: 0, label: '', value: '' }])
let nextKey = 1
function addItem() {
  items.value.push({ key: nextKey++, label: '', value: '' })
}
function removeItem(key: number) {
  items.value = items.value.filter(i => i.key !== key)
}
function handleSubmit() {
  message.success(`提交了 ${items.value.length} 项`)
}
</script>

<template>
  <NCard title="动态表单" :bordered="false">
    <NForm label-placement="left" label-width="60" style="max-width: 600px">
      <div v-for="item in items" :key="item.key" class="mb-3 flex items-start gap-2">
        <NFormItem label="标签" class="flex-1">
          <NInput v-model:value="item.label" placeholder="标签" />
        </NFormItem>
        <NFormItem label="值" class="flex-1">
          <NInput v-model:value="item.value" placeholder="值" />
        </NFormItem>
        <NButton
          v-if="items.length > 1"
          type="error"
          quaternary
          class="mt-1"
          @click="removeItem(item.key)"
        >
          删除
        </NButton>
      </div>
      <NSpace>
        <NButton dashed @click="addItem">+ 新增一行</NButton>
        <NButton type="primary" @click="handleSubmit">提交</NButton>
      </NSpace>
    </NForm>
  </NCard>
</template>
