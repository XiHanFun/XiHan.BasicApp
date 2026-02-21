<script lang="ts" setup>
import { NButton, NResult } from 'naive-ui'
import { useRouter } from 'vue-router'

defineOptions({ name: 'NotFoundPage' })

const router = useRouter()
</script>

<template>
  <div class="flex-col-center h-full min-h-[400px]">
    <NResult status="404" title="页面不存在" description="抱歉，您访问的页面不存在">
      <template #footer>
        <NButton type="primary" @click="router.replace('/')">返回首页</NButton>
        <NButton class="ml-3" @click="router.back()">返回上一页</NButton>
      </template>
    </NResult>
  </div>
</template>
