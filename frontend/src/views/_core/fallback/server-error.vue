<script lang="ts" setup>
import { NButton, NResult } from 'naive-ui'
import { useRouter } from 'vue-router'

defineOptions({ name: 'ServerErrorPage' })

const router = useRouter()
</script>

<template>
  <div class="flex-col-center h-full min-h-[400px]">
    <NResult status="500" title="服务器错误" description="抱歉，服务器发生了错误，请稍后重试">
      <template #footer>
        <NButton type="primary" @click="router.replace('/')">返回首页</NButton>
        <NButton class="ml-3" @click="router.go(0)">刷新重试</NButton>
      </template>
    </NResult>
  </div>
</template>
