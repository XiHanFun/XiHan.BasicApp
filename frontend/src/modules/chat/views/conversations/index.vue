<!--
  Copyright (c) 2021-Present XiHanFun and contributors.
  Licensed under the MIT License. See LICENSE in the project root for license information.
-->

<script setup lang="ts">
import ChatPanel from '../../components/ChatPanel.vue'

// 在线聊天全屏页（后端页面 chat.conversations，路由 /chat/conversations）；路由名由动态路由对齐，keepalive 按它匹配
defineOptions({ name: 'ChatConversationsView' })
</script>

<template>
  <!-- 布局内容区已是 definite 高度，直接 h-full 填满、ChatPanel 内部滚动，无需 JS 定高 -->
  <div class="h-full overflow-hidden p-2 sm:p-4">
    <ChatPanel mode="page" />
  </div>
</template>
