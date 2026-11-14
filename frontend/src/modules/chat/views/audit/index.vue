<!--
  Copyright (c) 2021-Present XiHanFun and contributors.
  Licensed under the MIT License. See LICENSE in the project root for license information.
-->

<script setup lang="ts">
import { XhTabsContent, XhTabsIndicator, XhTabsList, XhTabsRoot, XhTabsTrigger } from '@xihan-ui/vue'
import { ref } from 'vue'
import { useI18n } from 'vue-i18n'
import ChatAuditCallsTab from './components/ChatAuditCallsTab.vue'
import ChatAuditMessagesTab from './components/ChatAuditMessagesTab.vue'

/**
 * 聊天审计（后端页面 chat.audit，路由 /chat/audit）：持有审计权限即看当前一侧全部会话，不要求是成员
 * 一页两签：「消息」看消息、消息详情与会话成员，「通话记录」看通话的状态与参与者（没有通话内容）
 */
defineOptions({ name: 'ChatAuditView' })

const { t } = useI18n()

const activeTab = ref<'calls' | 'messages'>('messages')
</script>

<template>
  <div class="chat-audit-page">
    <!-- 两个面板各是一张列表，标签与面板手摆而不喂 collection -->
    <XhTabsRoot v-model:value="activeTab" variant="line" lazy-mount>
      <XhTabsList>
        <XhTabsTrigger value="messages">
          {{ t('chat.audit.tab_messages') }}
        </XhTabsTrigger>
        <XhTabsTrigger value="calls">
          {{ t('chat.audit.tab_calls') }}
        </XhTabsTrigger>
        <XhTabsIndicator />
      </XhTabsList>
      <XhTabsContent value="messages">
        <ChatAuditMessagesTab />
      </XhTabsContent>
      <XhTabsContent value="calls">
        <ChatAuditCallsTab />
      </XhTabsContent>
    </XhTabsRoot>
  </div>
</template>

<style scoped>
.chat-audit-page {
  block-size: 100%;
}

/* SchemaPage 依赖父级定高（内部 flex-1 + height:0 定高表格卡片，分页贴底），
   被标签页包裹时补全高度传递链，与根级 SchemaPage 页面布局一致 */
.chat-audit-page :deep([data-scope='tabs'][data-part='root']) {
  display: flex;
  flex-direction: column;
  block-size: 100%;
}

/* 标签条与下方 SchemaPage 内容左对齐，并留出上方呼吸空间 */
.chat-audit-page :deep([data-scope='tabs'][data-part='list']) {
  padding: var(--xh-space-2) var(--xh-space-3) 0;
}

.chat-audit-page :deep([data-scope='tabs'][data-part='content']) {
  flex: 1;
  block-size: 0;
  padding: 0;
}
</style>
