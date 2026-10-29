<script lang="ts" setup>
import type { NotificationPreference } from '~/types'
import { XhButton, XhButtonIndicator, XhButtonLabel, XhSpinner, XhSwitch } from '@xihan-ui/vue'
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { toast } from '~/composables'
import { Icon } from '~/iconify'
import { ApiError } from '~/request'
import { useAppContext } from '~/stores'

defineOptions({ name: 'ProfileTabNotifications' })

const { apis } = useAppContext()
const { t } = useI18n()

const loading = ref(false)
const saving = ref(false)
/** 与后端没保存过时的缺省一致（全收）；4.x 的推送、机器人、审批、营销 6.0 没有效果，不显示也不存 */
const pref = ref<NotificationPreference>({
  channelInApp: true,
  channelEmail: true,
  channelSms: true,
  typeAnnouncement: true,
  typeTask: true,
  typeSecurity: true,
})

interface PrefItem {
  key: keyof NotificationPreference
  label: string
  desc: string
  icon: string
}

const channels = computed<PrefItem[]>(() => [
  { key: 'channelInApp', label: t('component.profile.notifications.channel_in_app'), desc: t('component.profile.notifications.channel_in_app_desc'), icon: 'lucide:bell' },
  { key: 'channelEmail', label: t('component.profile.notifications.channel_email'), desc: t('component.profile.notifications.channel_email_desc'), icon: 'lucide:mail' },
  { key: 'channelSms', label: t('component.profile.notifications.channel_sms'), desc: t('component.profile.notifications.channel_sms_desc'), icon: 'lucide:smartphone' },
])

const types = computed<PrefItem[]>(() => [
  { key: 'typeAnnouncement', label: t('component.profile.notifications.type_announcement'), desc: t('component.profile.notifications.type_announcement_desc'), icon: 'lucide:megaphone' },
  { key: 'typeTask', label: t('component.profile.notifications.type_task'), desc: t('component.profile.notifications.type_task_desc'), icon: 'lucide:check-square' },
  { key: 'typeSecurity', label: t('component.profile.notifications.type_security'), desc: t('component.profile.notifications.type_security_desc'), icon: 'lucide:shield-alert' },
])

/** 服务端错误取本地化后的 detail（不带错误码前缀），其余取 message */
function messageOf(error: unknown): string | undefined {
  return error instanceof ApiError ? error.detail : (error as Error | undefined)?.message
}

async function loadPreference() {
  loading.value = true
  try {
    pref.value = await apis.getNotificationPreferenceApi()
  }
  catch (e: unknown) {
    toast.danger(messageOf(e) || t('component.profile.notifications.err_load_failed'))
  }
  finally {
    loading.value = false
  }
}

async function savePreference() {
  saving.value = true
  try {
    pref.value = await apis.updateNotificationPreferenceApi(pref.value)
    toast.success(t('component.profile.notifications.msg_saved'))
  }
  catch (e: unknown) {
    toast.danger(messageOf(e) || t('component.profile.notifications.err_save_failed'))
  }
  finally {
    saving.value = false
  }
}

onMounted(loadPreference)
</script>

<template>
  <div class="pf-tab-body">
    <div class="xh-loading-stage" :class="{ 'is-loading': loading }">
      <div class="xh-loading-stage__veil">
        <XhSpinner />
      </div>
      <section class="pf-section">
        <div class="pf-section__head">
          <div class="pf-section__heading">
            <div class="pf-section__title">
              {{ t('component.profile.notifications.section_channels') }}
            </div>
            <div class="pf-section__desc">
              {{ t('component.profile.notifications.section_channels_desc') }}
            </div>
          </div>
        </div>
        <div class="pf-section__body">
          <div class="pf-list">
            <div v-for="ch in channels" :key="ch.key" class="pf-list-item">
              <div class="pf-list-icon">
                <Icon :icon="ch.icon" width="16" />
              </div>
              <div class="pf-list-body">
                <div class="pf-list-title">
                  {{ ch.label }}
                </div>
                <div class="pf-list-desc">
                  {{ ch.desc }}
                </div>
              </div>
              <XhSwitch v-model:checked="pref[ch.key]" :aria-label="ch.label" />
            </div>
          </div>
        </div>
      </section>

      <section class="pf-section">
        <div class="pf-section__head">
          <div class="pf-section__heading">
            <div class="pf-section__title">
              {{ t('component.profile.notifications.section_types') }}
            </div>
            <div class="pf-section__desc">
              {{ t('component.profile.notifications.section_types_desc') }}
            </div>
          </div>
        </div>
        <div class="pf-section__body">
          <div class="pf-list">
            <div v-for="item in types" :key="item.key" class="pf-list-item">
              <div class="pf-list-icon">
                <Icon :icon="item.icon" width="16" />
              </div>
              <div class="pf-list-body">
                <div class="pf-list-title">
                  {{ item.label }}
                </div>
                <div class="pf-list-desc">
                  {{ item.desc }}
                </div>
              </div>
              <XhSwitch v-model:checked="pref[item.key]" :aria-label="item.label" />
            </div>
          </div>
        </div>
        <div class="pf-section__actions">
          <XhButton variant="subtle" @click="loadPreference">
            {{ t('common.actions.reset') }}
          </XhButton>
          <XhButton variant="subtle" tone="brand" :loading="saving" @click="savePreference">
            <XhButtonIndicator />
            <Icon icon="lucide:save" width="16" />
            <XhButtonLabel>{{ t('component.profile.notifications.btn_save_preference') }}</XhButtonLabel>
          </XhButton>
        </div>
      </section>
    </div>
  </div>
</template>

<style src="./profile-shared.css" />
