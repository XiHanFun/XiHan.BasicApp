<script lang="ts" setup>
import type { UserSessionItem } from '~/types'
import { XhButton, XhEmptyStateDescription, XhEmptyStateIcon, XhEmptyStateRoot, XhEmptyStateTitle, XhFlex, XhPopconfirmCancelTrigger, XhPopconfirmConfirmTrigger, XhPopconfirmContent, XhPopconfirmPositioner, XhPopconfirmRoot, XhPopconfirmTitle, XhPopconfirmTrigger, XhSpinner, XhTagLabel, XhTagRoot } from '@xihan-ui/vue'
import { onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { dialog, toast } from '~/composables'
import { Icon } from '~/iconify'
import { useAppContext } from '~/stores'
import { formatDate } from '~/utils'

defineOptions({ name: 'ProfileTabDevices' })

const { apis } = useAppContext()
const { t } = useI18n()

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
    toast.error((e as Error)?.message || t('component.profile.devices.err_load_failed'))
  }
  finally {
    sessionsLoading.value = false
  }
}

async function handleRevokeSession(sid: string) {
  try {
    await apis.revokeSessionApi(sid)
    toast.success(t('component.profile.devices.msg_device_logged_out'))
    await loadSessions()
  }
  catch (e: unknown) {
    toast.error((e as Error)?.message || t('component.profile.devices.err_operation_failed'))
  }
}

function handleRevokeOthers() {
  const cnt = sessions.value.filter(s => !s.isCurrent).length
  if (!cnt) {
    toast.info(t('component.profile.devices.info_no_other_devices'))
    return
  }
  void dialog.confirm({
    badge: 'warning',
    tone: 'danger',
    title: t('component.profile.devices.logout_all_title'),
    content: t('component.profile.devices.logout_all_content', { count: cnt }),
    okText: t('common.actions.confirm'),
    cancelText: t('common.actions.cancel'),
    onOk: async () => {
      try {
        await apis.revokeOtherSessionsApi()
        toast.success(t('component.profile.devices.msg_others_logged_out'))
        await loadSessions()
      }
      catch (e: unknown) {
        toast.error((e as Error)?.message || t('component.profile.devices.err_operation_failed'))
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
          <XhFlex gap="sm">
            <XhButton size="sm" variant="ghost" @click="loadSessions">
              <span><Icon icon="lucide:refresh-cw" /></span>
            </XhButton>
            <XhButton size="sm" @click="handleRevokeOthers">
              {{ t('component.profile.devices.btn_logout_others') }}
            </XhButton>
          </XhFlex>
        </div>
      </div>
      <div class="pf-section__body">
        <div class="xh-loading-stage" :class="{ 'is-loading': sessionsLoading }">
          <div class="xh-loading-stage__veil">
            <XhSpinner />
          </div>
          <XhEmptyStateRoot v-if="sessions.length === 0 && sessionsLoaded">
            <XhEmptyStateIcon>
              <Icon icon="lucide:inbox" width="28" height="28" />
            </XhEmptyStateIcon>
            <XhEmptyStateTitle>{{ t('common.no_data') }}</XhEmptyStateTitle>
            <XhEmptyStateDescription>{{ t('component.profile.devices.empty') }}</XhEmptyStateDescription>
          </XhEmptyStateRoot>
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
              <XhTagRoot v-if="s.isCurrent" variant="subtle" tone="success" size="sm">
                <XhTagLabel>
                  {{ t('component.profile.devices.tag_current') }}
                </XhTagLabel>
              </XhTagRoot>
              <XhPopconfirmRoot v-else @confirm="handleRevokeSession(s.sessionId)">
                <!-- 触发器本身就是那颗按钮：浮层触发器渲染成 button，不能再往里套一颗 -->
                <XhPopconfirmTrigger class="xh-linklike-trigger xh-linklike-trigger--danger">
                  {{ t('component.profile.devices.btn_kick') }}
                </XhPopconfirmTrigger>
                <XhPopconfirmPositioner>
                  <XhPopconfirmContent>
                    <XhPopconfirmTitle>
                      {{ t('component.profile.devices.confirm_logout_device') }}
                    </XhPopconfirmTitle>
                    <XhPopconfirmCancelTrigger>{{ t('common.actions.cancel') }}</XhPopconfirmCancelTrigger>
                    <XhPopconfirmConfirmTrigger>{{ t('common.actions.confirm') }}</XhPopconfirmConfirmTrigger>
                  </XhPopconfirmContent>
                </XhPopconfirmPositioner>
              </XhPopconfirmRoot>
            </div>
          </div>
        </div>
      </div>
    </section>
  </div>
</template>

<style src="./profile-shared.css" />
