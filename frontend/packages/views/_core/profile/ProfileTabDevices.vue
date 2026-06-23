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
import { useI18n } from 'vue-i18n'
import { Icon } from '~/iconify'
import { useAppContext } from '~/stores'
import { formatDate } from '~/utils'

defineOptions({ name: 'ProfileTabDevices' })

const { apis } = useAppContext()
const message = useMessage()
const { t } = useI18n()
const dialog = useDialog()

const sessionsLoading = ref(false)
const sessions = ref<UserSessionItem[]>([])
const sessionsLoaded = ref(false)

async function loadSessions() {
  sessionsLoading.value = true
  try {
    sessions.value = await apis.getSessionsApi()
    sessionsLoaded.value = true
  }
  catch (e: unknown) {
    message.error((e as Error)?.message || t('component.profile.devices.err_load_failed'))
  }
  finally {
    sessionsLoading.value = false
  }
}

async function handleRevokeSession(sid: string) {
  try {
    await apis.revokeSessionApi(sid)
    message.success(t('component.profile.devices.msg_device_logged_out'))
    await loadSessions()
  }
  catch (e: unknown) {
    message.error((e as Error)?.message || t('component.profile.devices.err_operation_failed'))
  }
}

function handleRevokeOthers() {
  const cnt = sessions.value.filter(s => !s.isCurrent).length
  if (!cnt) {
    message.info(t('component.profile.devices.info_no_other_devices'))
    return
  }
  dialog.warning({
    title: t('component.profile.devices.logout_all_title'),
    content: t('component.profile.devices.logout_all_content', { count: cnt }),
    positiveText: t('common.actions.confirm'),
    negativeText: t('common.actions.cancel'),
    onPositiveClick: async () => {
      try {
        await apis.revokeOtherSessionsApi()
        message.success(t('component.profile.devices.msg_others_logged_out'))
        await loadSessions()
      }
      catch (e: unknown) {
        message.error((e as Error)?.message || t('component.profile.devices.err_operation_failed'))
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
            <span>{{ t('component.profile.devices.section_title') }}</span>
          </div>
          <div class="pf-section__desc">
            {{ t('component.profile.devices.section_desc') }}
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
              {{ t('component.profile.devices.btn_logout_others') }}
            </NButton>
          </NSpace>
        </div>
      </div>
      <div class="pf-section__body">
        <NSpin :show="sessionsLoading">
          <NEmpty v-if="sessions.length === 0 && sessionsLoaded" :description="t('component.profile.devices.empty')" />
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
                  {{ s.deviceName || s.browser || t('component.profile.devices.unknown_device') }}
                </div>
                <div class="pf-list-desc">
                  {{ s.ipAddress }}
                  <template v-if="s.location">
                    · {{ s.location }}
                  </template>
                  <template v-if="s.operatingSystem">
                    · {{ s.operatingSystem }}
                  </template>
                  · {{ s.isCurrent ? t('component.profile.devices.status_online') : formatDate(s.lastActivityTime, 'MM-DD HH:mm') }}
                </div>
              </div>
              <NTag v-if="s.isCurrent" type="success" size="small" :bordered="false">
                {{ t('component.profile.devices.tag_current') }}
              </NTag>
              <NPopconfirm v-else @positive-click="handleRevokeSession(s.sessionId)">
                <template #trigger>
                  <NButton size="tiny" type="error" text>
                    {{ t('component.profile.devices.btn_kick') }}
                  </NButton>
                </template>
                {{ t('component.profile.devices.confirm_logout_device') }}
              </NPopconfirm>
            </div>
          </div>
        </NSpin>
      </div>
    </section>
  </div>
</template>

<style src="./profile-shared.css" />
