<script setup lang="ts">
import { ChatPanel } from '~/components'

// 路由名须与后端 PageRegistry 的 RouteName（MessageChat）一致，keepalive 按此匹配
defineOptions({ name: 'MessageChat' })
</script>

<template>
  <!-- overflow-hidden 钉死页面高度：消息再多也只允许面板内部（消息流/会话列表）滚动，不撑破外层页面滚动 -->
  <div class="h-full min-h-0 overflow-hidden p-2 sm:p-4">
    <ChatPanel mode="page" />
  </div>
</template>
