<script setup lang="ts">
import type { AppUserInboxDisplayItem } from '~/types'
import { XhButton, XhDialogCloseTrigger, XhDialogContent, XhDialogRoot, XhDialogTitle } from '@xihan-ui/vue'
import { computed, onMounted, ref } from 'vue'
import { useI18n } from 'vue-i18n'
import { NotificationContent } from '~/components'
import { useAppContext } from '~/stores'

defineOptions({ name: 'NotificationGate' })

const { t } = useI18n()
const appContext = useAppContext()

// ── 强制阅读：未读必读公告（最高优先级，遮罩拦截，逐条「我已阅读」） ──
const mandatoryList = ref<AppUserInboxDisplayItem[]>([])
const mandatoryVisible = computed(() => mandatoryList.value.length > 0)
const currentMandatory = computed<AppUserInboxDisplayItem | null>(() => mandatoryList.value[0] ?? null)
const markingId = ref<string | null>(null)

// ── 登录后弹窗：处理完强制阅读后逐条弹出 ──
const popupList = ref<AppUserInboxDisplayItem[]>([])
const popupIndex = ref(0)
const currentPopup = computed<AppUserInboxDisplayItem | null>(() => popupList.value[popupIndex.value] ?? null)
// 强制阅读未清空前不弹普通 popup
const popupVisible = computed(() => !mandatoryVisible.value && currentPopup.value !== null)

async function onMandatoryRead(item: AppUserInboxDisplayItem): Promise<void> {
  markingId.value = item.basicId
  try {
    await appContext.apis.userInboxApi.markRead(item.basicId)
    mandatoryList.value = mandatoryList.value.filter(i => i.basicId !== item.basicId)
  }
  catch {
    // 标记失败静默：保留该条，用户可重试
  }
  finally {
    markingId.value = null
  }
}

async function onPopupConfirm(item: AppUserInboxDisplayItem): Promise<void> {
  try {
    await appContext.apis.userInboxApi.markPopupShown(item.basicId)
  }
  catch {
    // 标记失败静默：仍前进到下一条，避免卡住
  }
  finally {
    popupIndex.value += 1
  }
}

onMounted(async () => {
  try {
    const [popup, mandatory] = await Promise.all([
      appContext.apis.userInboxApi.popup(),
      appContext.apis.userInboxApi.mandatoryUnread(),
    ])
    popupList.value = popup
    mandatoryList.value = mandatory
  }
  catch {
    // 拉取失败静默：不弹出即可
  }
})
</script>

<template>
  <!-- 强制阅读（不可关闭，遮罩拦截系统操作；逐条「我已阅读」清空后自动关闭） -->
  <XhDialogRoot
    v-if="currentMandatory"
    :open="mandatoryVisible"
    role="alertdialog"
    :close-on-escape="false"
    :close-on-interact-outside="false"
  >
    <XhDialogContent class="notif-gate-modal" style="--xh-dialog-max-w: 820px">
      <XhDialogTitle>{{ currentMandatory.title }}</XhDialogTitle>
      <div class="notif-gate-body">
        <NotificationContent
          v-if="currentMandatory.content"
          :content="currentMandatory.content"
          :format="currentMandatory.contentFormat"
        />
        <pre v-else class="notif-gate-text">{{ t('header.notification.gate.no_content') }}</pre>
      </div>
      <div class="flex justify-end">
        <XhButton
          size="sm"
          variant="solid"
          :loading="markingId === currentMandatory.basicId"
          @click="onMandatoryRead(currentMandatory)"
        >
          {{ t('header.notification.gate.mandatory_read') }}
        </XhButton>
      </div>
    </XhDialogContent>
  </XhDialogRoot>

  <!-- 登录后弹窗（逐条；点「我知道了」标记已展示并弹下一条） -->
  <XhDialogRoot
    v-if="currentPopup"
    :open="popupVisible"
    :close-on-interact-outside="false"
    @update:open="(open: boolean) => !open && onPopupConfirm(currentPopup!)"
  >
    <XhDialogContent class="notif-gate-modal" style="--xh-dialog-max-w: 820px">
      <XhDialogTitle>{{ currentPopup.title }}</XhDialogTitle>
      <XhDialogCloseTrigger />
      <div class="notif-gate-body">
        <NotificationContent
          v-if="currentPopup.content"
          :content="currentPopup.content"
          :format="currentPopup.contentFormat"
        />
        <pre v-else class="notif-gate-text">{{ t('header.notification.gate.no_content') }}</pre>
      </div>
      <div class="flex justify-end">
        <XhButton size="sm" variant="solid" @click="onPopupConfirm(currentPopup)">
          {{ t('header.notification.gate.popup_ok') }}
        </XhButton>
      </div>
    </XhDialogContent>
  </XhDialogRoot>
</template>

<style scoped>
.notif-gate-body {
  max-height: 60vh;
  overflow-y: auto;
  font-size: 14px;
  color: hsl(var(--foreground) / 85%);
}

.notif-gate-text {
  margin: 0;
  white-space: pre-wrap;
  word-break: break-word;
}
</style>
