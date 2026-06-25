<script setup lang="ts">
import type { AppUserInboxDisplayItem } from '~/types'
import { NButton, NModal } from 'naive-ui'
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
  <NModal
    v-if="currentMandatory"
    :show="mandatoryVisible"
    preset="card"
    :title="currentMandatory.title"
    :mask-closable="false"
    :closable="false"
    :close-on-esc="false"
    style="width: 600px; max-width: 92vw"
  >
    <div class="notif-gate-body">
      <NotificationContent
        v-if="currentMandatory.content"
        :content="currentMandatory.content"
        :format="currentMandatory.contentFormat"
      />
      <pre v-else class="notif-gate-text">{{ t('header.notification.gate.no_content') }}</pre>
    </div>
    <template #footer>
      <div class="flex justify-end">
        <NButton
          size="small"
          type="primary"
          :loading="markingId === currentMandatory.basicId"
          @click="onMandatoryRead(currentMandatory)"
        >
          {{ t('header.notification.gate.mandatory_read') }}
        </NButton>
      </div>
    </template>
  </NModal>

  <!-- 登录后弹窗（逐条；点「我知道了」标记已展示并弹下一条） -->
  <NModal
    v-if="currentPopup"
    :show="popupVisible"
    preset="card"
    :title="currentPopup.title"
    :mask-closable="false"
    style="width: 560px; max-width: 92vw"
    @close="onPopupConfirm(currentPopup)"
  >
    <div class="notif-gate-body">
      <NotificationContent
        v-if="currentPopup.content"
        :content="currentPopup.content"
        :format="currentPopup.contentFormat"
      />
      <pre v-else class="notif-gate-text">{{ t('header.notification.gate.no_content') }}</pre>
    </div>
    <template #footer>
      <div class="flex justify-end">
        <NButton size="small" type="primary" @click="onPopupConfirm(currentPopup)">
          {{ t('header.notification.gate.popup_ok') }}
        </NButton>
      </div>
    </template>
  </NModal>
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
