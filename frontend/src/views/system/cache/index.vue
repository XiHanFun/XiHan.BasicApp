<script setup lang="ts">
import {
  NButton,
  NCard,
  NForm,
  NFormItem,
  NInput,
  NInputNumber,
  NSpace,
  useMessage,
} from 'naive-ui'
import { ref } from 'vue'
import { cacheApi } from '@/api'
import { XJsonViewer } from '~/components'

defineOptions({ name: 'SystemCachePage' })

const message = useMessage()
const key = ref('')
const value = ref('')
const expireSeconds = ref(300)
const pattern = ref('*')
const output = ref('')

function setOutput(title: string, payload: unknown) {
  output.value = `${title}\n${JSON.stringify(payload, null, 2)}`
}

async function handleGet() {
  if (!key.value.trim()) {
    message.warning('请输入缓存键')
    return
  }
  const result = await cacheApi.getString(key.value.trim())
  setOutput('缓存值', result)
}

async function handleSet() {
  if (!key.value.trim()) {
    message.warning('请输入缓存键')
    return
  }
  await cacheApi.setString({
    key: key.value.trim(),
    value: value.value,
    expireSeconds: expireSeconds.value,
  })
  message.success('设置成功')
}

async function handleExists() {
  if (!key.value.trim()) {
    message.warning('请输入缓存键')
    return
  }
  const exists = await cacheApi.exists(key.value.trim())
  setOutput('键存在检查', { key: key.value.trim(), exists })
}

async function handleRemove() {
  if (!key.value.trim()) {
    message.warning('请输入缓存键')
    return
  }
  await cacheApi.remove(key.value.trim())
  message.success('删除成功')
}

async function handleKeys() {
  const keys = await cacheApi.getKeys(pattern.value || '*')
  setOutput('键列表', keys)
}

async function handleRemoveByPattern() {
  const count = await cacheApi.removeByPattern(pattern.value || '*')
  setOutput('按模式删除结果', { removedCount: count })
}
</script>

<template>
  <div class="space-y-3">
    <NCard title="缓存管理" :bordered="false" size="small">
      <NForm label-placement="left" label-width="90px">
        <NFormItem label="缓存键">
          <NInput v-model:value="key" placeholder="如: auth:user:1001" />
        </NFormItem>
        <NFormItem label="缓存值">
          <NInput
            v-model:value="value"
            type="textarea"
            :rows="3"
            placeholder="请输入缓存字符串值"
          />
        </NFormItem>
        <NFormItem label="过期秒数">
          <NInputNumber v-model:value="expireSeconds" :min="1" :max="86400" />
        </NFormItem>
        <NFormItem label="键模式">
          <NInput v-model:value="pattern" placeholder="如: auth:*" />
        </NFormItem>
      </NForm>

      <NSpace wrap>
        <NButton size="small" type="primary" @click="handleGet">
          读取
        </NButton>
        <NButton size="small" type="primary" ghost @click="handleSet">
          写入
        </NButton>
        <NButton size="small" @click="handleExists">
          存在检查
        </NButton>
        <NButton size="small" type="error" ghost @click="handleRemove">
          删除键
        </NButton>
        <NButton size="small" @click="handleKeys">
          查询键列表
        </NButton>
        <NButton size="small" type="warning" ghost @click="handleRemoveByPattern">
          按模式删除
        </NButton>
      </NSpace>
    </NCard>

    <XJsonViewer title="执行结果" :raw-text="output || '暂无执行结果'" :max-height="320" />
  </div>
</template>
