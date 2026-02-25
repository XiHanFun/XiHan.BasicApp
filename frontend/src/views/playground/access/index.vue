<script setup lang="ts">
import { NCard, NTag } from 'naive-ui'
import { useUserStore } from '~/stores'

const userStore = useUserStore()
const canManageUser = userStore.hasPermission('system:user:list')
</script>

<template>
  <NCard title="权限示例" :bordered="false" class="space-y-3">
    <p>当前角色：{{ userStore.roles.join(', ') || '无' }}</p>
    <p>当前权限数：{{ userStore.permissions.length }}</p>
    <NTag :type="canManageUser ? 'success' : 'error'">
      {{ canManageUser ? '可访问 用户管理' : '不可访问 用户管理' }}
    </NTag>
  </NCard>
</template>
