<script setup lang="ts">
import { ChatPanel } from '~/components'

// 路由名须与后端 PageRegistry 的 RouteName（MessageChat）一致，keepalive 按此匹配
defineOptions({ name: 'MessageChat' })
</script>

<template>
  <div class="h-full p-4">
    <ChatPanel mode="page" />
  </div>
</template>
