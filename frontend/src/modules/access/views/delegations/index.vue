<script lang="ts" setup>
import { XhTabsContent, XhTabsIndicator, XhTabsList, XhTabsRoot, XhTabsTrigger } from '@xihan-ui/vue'
import { computed, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import { useAuthStore } from '~/stores'
import DelegationsTab from './components/DelegationsTab.vue'
import PermissionRequestsTab from './components/PermissionRequestsTab.vue'

/**
 * 授权申请页（一页两签，设计稿 D3）：页面码与路由仍是 access.delegations / /access/delegations，
 * 菜单权限是 access.permission-requests.read；「申请」页签在前，「委托」页签按 access.delegations.read 显示
 */
defineOptions({ name: 'AccessDelegationsView' })

const { t } = useI18n()
const auth = useAuthStore()

const activeTab = ref<'request' | 'delegation'>('request')
const showDelegations = computed(() => auth.can('access.delegations.read'))

// 权限刷新后失去委托的读取权时，停在委托页签上会是空白：退回申请页签
watch(showDelegations, (visible) => {
  if (!visible && activeTab.value === 'delegation')
    activeTab.value = 'request'
})
</script>

<template>
  <div class="auth-page">
    <!-- 面板内容各不相同，标签与面板手摆而不喂 collection -->
    <XhTabsRoot v-model:value="activeTab" variant="line">
      <XhTabsList>
        <XhTabsTrigger value="request">
          {{ t('access.permission_requests.tab') }}
        </XhTabsTrigger>
        <XhTabsTrigger v-if="showDelegations" value="delegation">
          {{ t('access.delegations.tab') }}
        </XhTabsTrigger>
        <XhTabsIndicator />
      </XhTabsList>
      <XhTabsContent value="request">
        <PermissionRequestsTab />
      </XhTabsContent>
      <XhTabsContent v-if="showDelegations" value="delegation">
        <DelegationsTab />
      </XhTabsContent>
    </XhTabsRoot>
  </div>
</template>

<style scoped>
.auth-page {
  height: 100%;
}

/* SchemaPage 依赖父级定高（内部 flex-1 + height:0 定高表格卡片，分页贴底），
   被标签页包裹时补全高度传递链，与根级 SchemaPage 页面布局一致 */
.auth-page :deep([data-scope='tabs'][data-part='root']) {
  display: flex;
  flex-direction: column;
  height: 100%;
}

/* 标签条与下方 SchemaPage 内容左对齐，并留出上方呼吸空间 */
.auth-page :deep([data-scope='tabs'][data-part='list']) {
  padding: var(--xh-space-2) var(--xh-space-3) 0;
}

.auth-page :deep([data-scope='tabs'][data-part='content']) {
  flex: 1;
  height: 0;
  padding: 0;
}
</style>
