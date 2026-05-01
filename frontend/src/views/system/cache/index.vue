<script setup lang="ts">
import type { CacheExistsResult, CacheRemoveByPatternResult } from '@/api'
import {
  NButton,
  NCard,
  NForm,
  NFormItem,
  NGi,
  NGrid,
  NIcon,
  NInput,
  NInputNumber,
  NSpace,
  NTooltip,
  useMessage,
} from 'naive-ui'
import { computed, ref } from 'vue'
import { cacheApi } from '@/api'
import { Icon, XJsonViewer } from '~/components'

defineOptions({ name: 'SystemCachePage' })

const message = useMessage()

const cacheKey = ref('')
const cacheValue = ref('')
const expireSeconds = ref(300)
const keyPattern = ref('*')
const loadingAction = ref<CacheAction | null>(null)
const outputTitle = ref('执行结果')
const outputValue = ref<unknown>(null)

type CacheAction = 'exists' | 'get' | 'keys' | 'remove' | 'removePattern' | 'set'

const trimmedKey = computed(() => cacheKey.value.trim())
const normalizedPattern = computed(() => keyPattern.value.trim() || '*')
const outputText = computed(() => {
  if (outputValue.value === null) {
    return 'null'
  }

  if (outputValue.value === undefined) {
    return '暂无执行结果'
  }

  return JSON.stringify(outputValue.value, null, 2)
})

function requireKey() {
  if (trimmedKey.value) {
    return trimmedKey.value
  }

  message.warning('请输入缓存键')
  return null
}

async function runCacheAction(action: CacheAction, callback: () => Promise<void>) {
  loadingAction.value = action

  try {
    await callback()
  }
  finally {
    loadingAction.value = null
  }
}

function setOutput(title: string, payload: unknown) {
  outputTitle.value = title
  outputValue.value = payload
}

async function handleGet() {
  const key = requireKey()
  if (!key) {
    return
  }

  await runCacheAction('get', async () => {
    const value = await cacheApi.getString(key)
    setOutput('缓存值', { key, value })
  })
}

async function handleSet() {
  const key = requireKey()
  if (!key) {
    return
  }

  await runCacheAction('set', async () => {
    await cacheApi.setString({
      expireSeconds: expireSeconds.value,
      key,
      value: cacheValue.value,
    })
    message.success('写入成功')
    setOutput('写入结果', { expireSeconds: expireSeconds.value, key, saved: true })
  })
}

async function handleExists() {
  const key = requireKey()
  if (!key) {
    return
  }

  await runCacheAction('exists', async () => {
    const exists = await cacheApi.exists(key)
    const result: CacheExistsResult = { exists, key }
    setOutput('键存在检查', result)
  })
}

async function handleRemove() {
  const key = requireKey()
  if (!key) {
    return
  }

  await runCacheAction('remove', async () => {
    await cacheApi.remove(key)
    message.success('删除成功')
    setOutput('删除结果', { key, removed: true })
  })
}

async function handleKeys() {
  await runCacheAction('keys', async () => {
    const keys = await cacheApi.getKeys(normalizedPattern.value)
    setOutput('键列表', keys)
  })
}

async function handleRemoveByPattern() {
  await runCacheAction('removePattern', async () => {
    const removedCount = await cacheApi.removeByPattern(normalizedPattern.value)
    const result: CacheRemoveByPatternResult = {
      pattern: normalizedPattern.value,
      removedCount,
    }
    setOutput('按模式删除结果', result)
  })
}
</script>

<template>
  <div class="system-cache-page">
    <NGrid :cols="1" :x-gap="12" :y-gap="12" responsive="screen">
      <NGi>
        <NCard title="缓存管理" :bordered="false" size="small">
          <NForm label-placement="left" label-width="92px">
            <NFormItem label="缓存键">
              <NInput v-model:value="cacheKey" clearable placeholder="auth:user:1001" />
            </NFormItem>
            <NFormItem label="缓存值">
              <NInput
                v-model:value="cacheValue"
                clearable
                placeholder="请输入缓存字符串值"
                :rows="4"
                type="textarea"
              />
            </NFormItem>
            <NFormItem label="过期秒数">
              <NInputNumber v-model:value="expireSeconds" :max="604800" :min="1" />
            </NFormItem>
            <NFormItem label="键模式">
              <NInput v-model:value="keyPattern" clearable placeholder="auth:*" />
            </NFormItem>
          </NForm>

          <NSpace wrap>
            <NTooltip>
              <template #trigger>
                <NButton
                  :loading="loadingAction === 'get'"
                  size="small"
                  type="primary"
                  @click="handleGet"
                >
                  <template #icon>
                    <NIcon><Icon icon="lucide:download" /></NIcon>
                  </template>
                  读取
                </NButton>
              </template>
              读取指定缓存键
            </NTooltip>

            <NTooltip>
              <template #trigger>
                <NButton
                  :loading="loadingAction === 'set'"
                  ghost
                  size="small"
                  type="primary"
                  @click="handleSet"
                >
                  <template #icon>
                    <NIcon><Icon icon="lucide:save" /></NIcon>
                  </template>
                  写入
                </NButton>
              </template>
              写入字符串缓存值
            </NTooltip>

            <NTooltip>
              <template #trigger>
                <NButton :loading="loadingAction === 'exists'" size="small" @click="handleExists">
                  <template #icon>
                    <NIcon><Icon icon="lucide:circle-check" /></NIcon>
                  </template>
                  存在检查
                </NButton>
              </template>
              检查指定缓存键是否存在
            </NTooltip>

            <NTooltip>
              <template #trigger>
                <NButton
                  :loading="loadingAction === 'remove'"
                  ghost
                  size="small"
                  type="error"
                  @click="handleRemove"
                >
                  <template #icon>
                    <NIcon><Icon icon="lucide:trash-2" /></NIcon>
                  </template>
                  删除键
                </NButton>
              </template>
              删除指定缓存键
            </NTooltip>

            <NTooltip>
              <template #trigger>
                <NButton :loading="loadingAction === 'keys'" size="small" @click="handleKeys">
                  <template #icon>
                    <NIcon><Icon icon="lucide:list" /></NIcon>
                  </template>
                  查询键列表
                </NButton>
              </template>
              按模式查询缓存键
            </NTooltip>

            <NTooltip>
              <template #trigger>
                <NButton
                  :loading="loadingAction === 'removePattern'"
                  ghost
                  size="small"
                  type="warning"
                  @click="handleRemoveByPattern"
                >
                  <template #icon>
                    <NIcon><Icon icon="lucide:eraser" /></NIcon>
                  </template>
                  按模式删除
                </NButton>
              </template>
              按模式批量删除缓存键
            </NTooltip>
          </NSpace>
        </NCard>
      </NGi>

      <NGi>
        <XJsonViewer :title="outputTitle" :raw-text="outputText" :max-height="360" />
      </NGi>
    </NGrid>
  </div>
</template>

<style scoped>
.system-cache-page {
  width: 100%;
}
</style>
