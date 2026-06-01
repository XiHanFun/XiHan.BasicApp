<script lang="ts" setup>
import type { UserSessionItem } from '~/types'
import {
  NButton,
  NEmpty,
  NIcon,
  NPopconfirm,
  NSpace,
  NSpin,
  NTag,
  useDialog,
  useMessage,
} from 'naive-ui'
import { onMounted, ref } from 'vue'
import { Icon } from '~/iconify'
import { useAppContext } from '~/stores'
import { formatDate } from '~/utils'

defineOptions({ name: 'ProfileTabDevices' })

const { apis } = useAppContext()
const message = useMessage()
const dialog = useDialog()

const sessionsLoading = ref(false)
const sessions = ref<UserSessionItem[]>([])
const sessionsLoaded = ref(false)

// 暴露给父组件（Banner 在线设备计数）
defineExpose({ sessions, sessionsLoaded })

async function loadSessions() {
  sessionsLoading.value = true
  try {
    sessions.value = await apis.getSessionsApi()
    sessionsLoaded.value = true
  }
  catch (e: unknown) {
    message.error((e as Error)?.message || '加载失败')
  }
  finally {
    sessionsLoading.value = false
  }
}

async function handleRevokeSession(sid: string) {
  try {
    await apis.revokeSessionApi(sid)
    message.success('设备已登出')
    await loadSessions()
  }
  catch (e: unknown) {
    message.error((e as Error)?.message || '操作失败')
  }
}

function handleRevokeOthers() {
  const cnt = sessions.value.filter(s => !s.isCurrent).length
  if (!cnt) {
    message.info('没有其他在线设备')
    return
  }
  dialog.warning({
    title: '登出所有设备',
    content: `将下线除当前设备外的 ${cnt} 个设备，是否继续？`,
    positiveText: '确认',
    negativeText: '取消',
    onPositiveClick: async () => {
      try {
        await apis.revokeOtherSessionsApi()
        message.success('已登出所有其他设备')
        await loadSessions()
      }
      catch (e: unknown) {
        message.error((e as Error)?.message || '操作失败')
      }
    },
  })
}

function deviceIcon(t: number) {
  const map: Record<number, string> = {
    1: 'lucide:globe',
    2: 'lucide:smartphone',
    3: 'lucide:monitor',
    4: 'lucide:tablet',
  }
  return map[t] || 'lucide:help-circle'
}

onMounted(loadSessions)
</script>

<template>
  <div class="pf-tab-body">
    <section class="pf-section">
      <div class="pf-section__head">
        <div class="pf-section__heading">
          <div class="pf-section__title">
            <Icon icon="lucide:monitor-smartphone" width="16" />
            <span>登录设备管理</span>
          </div>
          <div class="pf-section__desc">
            查看当前账号已登录的设备，可远程登出可疑设备。
          </div>
        </div>
        <div class="pf-section__extra">
          <NSpace :size="8">
            <NButton size="tiny" quaternary @click="loadSessions">
              <template #icon>
                <NIcon>
                  <Icon icon="lucide:refresh-cw" />
                </NIcon>
              </template>
            </NButton>
            <NButton size="tiny" @click="handleRevokeOthers">
              登出其他设备
            </NButton>
          </NSpace>
        </div>
      </div>
      <div class="pf-section__body">
        <NSpin :show="sessionsLoading">
          <NEmpty v-if="sessions.length === 0 && sessionsLoaded" description="暂无在线设备" />
          <div v-else class="pf-list">
            <div
              v-for="s in sessions"
              :key="s.sessionId"
              class="pf-list-item"
              :class="{ 'pf-list-item--active': s.isCurrent }"
            >
              <div class="pf-list-icon" :class="{ 'pf-list-icon--active': s.isCurrent }">
                <Icon :icon="deviceIcon(s.deviceType)" width="16" />
              </div>
              <div class="pf-list-body">
                <div class="pf-list-title">
                  {{ s.deviceName || s.browser || '未知设备' }}
                </div>
                <div class="pf-list-desc">
                  {{ s.ipAddress }}
                  <template v-if="s.location">
                    · {{ s.location }}
                  </template>
                  <template v-if="s.operatingSystem">
                    · {{ s.operatingSystem }}
                  </template>
                  · {{ s.isCurrent ? '在线' : formatDate(s.lastActivityTime, 'MM-DD HH:mm') }}
                </div>
              </div>
              <NTag v-if="s.isCurrent" type="success" size="small" :bordered="false">
                当前
              </NTag>
              <NPopconfirm v-else @positive-click="handleRevokeSession(s.sessionId)">
                <template #trigger>
                  <NButton size="tiny" type="error" text>
                    踢下线
                  </NButton>
                </template>
                确定登出该设备？
              </NPopconfirm>
            </div>
          </div>
        </NSpin>
      </div>
    </section>
  </div>
</template>

<style src="./profile-shared.css" />
