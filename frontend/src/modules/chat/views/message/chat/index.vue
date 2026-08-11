<script setup lang="ts">
import { ChatPanel } from '~/components'

// 路由名须与后端 PageRegistry 的 RouteName（MessageChat）一致，keepalive 按此匹配
defineOptions({ name: 'MessageChat' })
</script>

<template>
  <!-- 布局内容区已是 definite 高度，直接 h-full 填满、ChatPanel 内部滚动，无需 JS 定高 -->
  <div class="h-full overflow-hidden p-2 sm:p-4">
    <ChatPanel mode="page" />
  </div>
</template>
