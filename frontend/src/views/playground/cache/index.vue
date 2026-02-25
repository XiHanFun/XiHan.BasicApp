<script setup lang="ts">
import { NButton, NCard, NInput, NSpace } from 'naive-ui'
import { computed, ref } from 'vue'
import { storage } from '~/utils'

const key = 'playground_cache_text'
const input = ref(storage.get<string>(key) ?? '')
const cached = computed(() => storage.get<string>(key) ?? '')

function saveCache() {
  storage.set(key, input.value)
}

function clearCache() {
  storage.remove(key)
  input.value = ''
}
</script>

<template>
  <NCard title="缓存示例" :bordered="false" class="space-y-4">
    <NInput v-model:value="input" placeholder="输入内容并写入 localStorage" />
    <NSpace>
      <NButton type="primary" @click="saveCache">写入缓存</NButton>
      <NButton @click="clearCache">清空缓存</NButton>
    </NSpace>
    <p>当前缓存值：{{ cached || '（空）' }}</p>
  </NCard>
</template>
