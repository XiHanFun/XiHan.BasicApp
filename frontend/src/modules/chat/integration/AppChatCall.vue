<!--
  Copyright (c) 2021-Present XiHanFun and contributors.
  Licensed under the MIT License. See LICENSE in the project root for license information.
-->

<script setup lang="ts">
import type { FloatingPanelPosition, FloatingPanelSize, FloatingPanelWindowState } from '@xihan-ui/headless'
import type { ChatCallNoticeKind } from '../call/call-store'
import {
  XhButton,
  XhFloatingPanelBody,
  XhFloatingPanelContent,
  XhFloatingPanelDragTrigger,
  XhFloatingPanelHeader,
  XhFloatingPanelPositioner,
  XhFloatingPanelRoot,
  XhFloatingPanelTitle,
  XhFloatingPanelWindowStateTrigger,
  XhPopoverContent,
  XhPopoverPositioner,
  XhPopoverRoot,
  XhPopoverTrigger,
} from '@xihan-ui/vue'
import { computed, onBeforeUnmount, ref, watch } from 'vue'
import { useI18n } from 'vue-i18n'
import XUserAvatar from '~/components/common/UserAvatar.vue'
import XIconButton from '~/components/common/XIconButton.vue'
import XSelect from '~/components/common/XSelect.vue'
import { toast, useIsMobile } from '~/composables'
import { Icon } from '~/iconify'
import { callMediaFailureText, callMediaLabel, callParticipantName, formatCallDuration } from '../call/call-helpers'
import { ChatCallMediaError, listCallDevices } from '../call/call-media'
import { useChatCallStore } from '../call/call-store'
import { chatErrorText } from '../components/chat-helpers'

/**
 * 通话的全局界面：来电卡片与通话窗，挂在布局根部，任何页面都能接听，离开聊天页通话不断
 *
 * 来电卡片在每个开着的页签都出现；通话窗只在占着这通电话的页签（发起了或接听了的那个）出现，
 * 非模态、可拖动、可收拢成标题栏或铺满视口，通话期间照常用页面
 */
defineOptions({ name: 'AppChatCall' })

/** 通话窗与视口右缘、上缘的留白（px） */
const GAP = 16
/** 顶栏与标签栏下方再落位，不压住它们的按钮（px） */
const TOP = 96
const VOICE_SIZE: FloatingPanelSize = { width: 320, height: 380 }
const VIDEO_SIZE: FloatingPanelSize = { width: 520, height: 440 }
const MIN_SIZE: FloatingPanelSize = { width: 280, height: 300 }

const { t } = useI18n()
const callStore = useChatCallStore()
const { isMobile } = useIsMobile()

const call = computed(() => callStore.call)
const isVideo = computed(() => call.value?.media === 'Video')
const peerName = computed(() => callParticipantName(callStore.peer) || t('chat.call.unknown_peer'))
const mediaLabel = computed(() => callMediaLabel(t, call.value?.media ?? 'Voice'))
const incoming = computed(() => callStore.phase === 'incoming')
/** 占着这通电话的页签才开通话窗 */
const windowOpen = computed(() => callStore.engaged && call.value !== null)

// ── 通话时长：连上之后每秒走一格 ─────────────────────────────────────
const now = ref(Date.now())
let ticker: null | ReturnType<typeof setInterval> = null

watch(() => callStore.connectedAt, (connectedAt) => {
  if (ticker) {
    clearInterval(ticker)
    ticker = null
  }
  if (connectedAt !== null) {
    now.value = Date.now()
    ticker = setInterval(() => {
      now.value = Date.now()
    }, 1000)
  }
}, { immediate: true })

onBeforeUnmount(() => {
  if (ticker) {
    clearInterval(ticker)
  }
})

const statusText = computed(() => {
  switch (callStore.phase) {
    case 'outgoing':
      return t('chat.call.status_ringing')
    case 'connecting':
      return t('chat.call.status_connecting')
    case 'connected':
      return formatCallDuration(((now.value - (callStore.connectedAt ?? now.value)) / 1000))
    default:
      return ''
  }
})

// ── 通话窗：位置、尺寸与形态受控，每通电话重新落位 ───────────────────
const windowState = ref<FloatingPanelWindowState>('default')
const position = ref<FloatingPanelPosition>({ x: GAP, y: TOP })
const dimensions = ref<FloatingPanelSize>({ ...VOICE_SIZE })

watch(windowOpen, (open) => {
  if (!open) {
    return
  }
  const preferred = isVideo.value
    ? VIDEO_SIZE
    : VOICE_SIZE
  const width = Math.max(MIN_SIZE.width, Math.min(preferred.width, window.innerWidth - GAP * 2))
  const height = Math.max(MIN_SIZE.height, Math.min(preferred.height, window.innerHeight - TOP - GAP))
  dimensions.value = { width, height }
  position.value = {
    x: Math.max(GAP, window.innerWidth - width - GAP),
    y: TOP,
  }
  // 小屏放不下一扇浮窗，直接铺满
  windowState.value = isMobile.value
    ? 'maximized'
    : 'default'
})

// ── 媒体元素：对端的声音走隐藏的音频元素，画面走静音的视频元素 ─────
const remoteAudio = ref<HTMLAudioElement | null>(null)
const remoteVideo = ref<HTMLVideoElement | null>(null)
const localVideo = ref<HTMLVideoElement | null>(null)

function attach(element: HTMLMediaElement | null, stream: MediaStream | null): void {
  if (element && element.srcObject !== stream) {
    element.srcObject = stream
  }
}

watch([
  remoteAudio,
  () => callStore.remoteStream,
], ([element, stream]) => attach(element, stream), { flush: 'post' })
watch([
  remoteVideo,
  () => callStore.remoteStream,
], ([element, stream]) => attach(element, stream), { flush: 'post' })
watch([
  localVideo,
  () => callStore.localStream,
], ([element, stream]) => attach(element, stream), { flush: 'post' })

/** 对端的画面看得到：视频通话、对端开着摄像头、媒体已经到了 */
const showRemoteVideo = computed(() => isVideo.value && callStore.peerCameraOn && callStore.remoteStream !== null)
const showLocalVideo = computed(() => isVideo.value && callStore.cameraOn && callStore.localStream !== null)

// ── 设备：通话中换麦克风、摄像头与扬声器 ────────────────────────────
interface DeviceOption { label: string, value: string }

const devicesOpen = ref(false)
const microphones = ref<DeviceOption[]>([])
const cameras = ref<DeviceOption[]>([])
const speakers = ref<DeviceOption[]>([])
const microphoneId = ref<null | string>(null)
const cameraId = ref<null | string>(null)
const speakerId = ref<null | string>(null)

function optionsOf(devices: MediaDeviceInfo[], fallback: string): DeviceOption[] {
  return devices.map((device, index) => ({
    label: device.label || `${fallback} ${index + 1}`,
    value: device.deviceId,
  }))
}

watch(devicesOpen, async (open) => {
  if (!open) {
    return
  }
  const devices = await listCallDevices()
  microphones.value = optionsOf(devices.microphones, t('chat.call.microphone'))
  cameras.value = optionsOf(devices.cameras, t('chat.call.camera'))
  speakers.value = optionsOf(devices.speakers, t('chat.call.speaker'))
  // 选中项取正在用的轨道所属的设备
  const stream = callStore.localStream
  microphoneId.value = stream?.getAudioTracks()[0]?.getSettings().deviceId ?? null
  cameraId.value = stream?.getVideoTracks()[0]?.getSettings().deviceId ?? null
})

function mediaErrorText(error: unknown): string {
  if (error instanceof ChatCallMediaError) {
    return callMediaFailureText(t, error.failure)
  }
  return chatErrorText(error) || t('common.messages.operation_failed')
}

async function handleSwitchDevice(kind: 'audioinput' | 'videoinput', value: unknown): Promise<void> {
  if (typeof value !== 'string' || value === '') {
    return
  }
  try {
    await callStore.switchDevice(kind, value)
    if (kind === 'audioinput') {
      microphoneId.value = value
    }
    else {
      cameraId.value = value
    }
  }
  catch (error) {
    toast.danger(mediaErrorText(error))
  }
}

async function handleSwitchSpeaker(value: unknown): Promise<void> {
  const element = remoteAudio.value
  if (typeof value !== 'string' || !element || !('setSinkId' in element)) {
    return
  }
  try {
    await element.setSinkId(value)
    speakerId.value = value
  }
  catch (error) {
    toast.danger(mediaErrorText(error))
  }
}

// 每通电话从系统缺省的设备开始
watch(windowOpen, (open) => {
  if (!open) {
    devicesOpen.value = false
    speakerId.value = null
  }
})

// ── 动作 ────────────────────────────────────────────────────────────
async function handleAccept(): Promise<void> {
  try {
    await callStore.accept()
  }
  catch (error) {
    toast.danger(mediaErrorText(error))
  }
}

async function handleReject(): Promise<void> {
  try {
    await callStore.reject()
  }
  catch (error) {
    toast.danger(mediaErrorText(error))
  }
}

async function handleHangup(): Promise<void> {
  try {
    await callStore.hangup()
  }
  catch (error) {
    toast.danger(mediaErrorText(error))
  }
}

// ── 收场提示：对方拒绝、没人接、对方取消、连不上与中断 ───────────────
const NOTICE_KEYS: Record<ChatCallNoticeKind, string> = {
  'rejected': 'chat.call.notice_rejected',
  'no-answer': 'chat.call.notice_no_answer',
  'cancelled': 'chat.call.notice_cancelled',
  'interrupted': 'chat.call.notice_interrupted',
  'connect-failed': 'chat.call.notice_connect_failed',
}

watch(() => callStore.notice, (notice) => {
  if (!notice) {
    return
  }
  const text = t(NOTICE_KEYS[notice.kind], { name: notice.peerName || t('chat.call.unknown_peer') })
  if (notice.kind === 'connect-failed' || notice.kind === 'interrupted') {
    toast.warning(text)
  }
  else {
    toast.info(text)
  }
  callStore.notice = null
})
</script>

<template>
  <!-- 来电卡片：没接之前在每个页签都出现，接听、拒绝或对方取消后收起 -->
  <Transition name="app-fade">
    <div
      v-if="incoming"
      class="app-chat-call__incoming"
      role="alertdialog"
      aria-labelledby="app-chat-call-incoming-title"
      aria-describedby="app-chat-call-incoming-detail"
    >
      <XUserAvatar :avatar="callStore.peer?.avatarFileId ?? null" :name="peerName" :size="44" />
      <div class="min-w-0 flex-1">
        <div id="app-chat-call-incoming-title" class="truncate text-sm font-semibold text-foreground">
          {{ peerName }}
        </div>
        <div id="app-chat-call-incoming-detail" class="truncate text-xs text-muted-foreground">
          {{ isVideo ? t('chat.call.incoming_video') : t('chat.call.incoming_voice') }}
        </div>
      </div>
      <XhButton
        tone="danger"
        variant="solid"
        size="sm"
        icon-only
        :aria-label="t('chat.call.reject')"
        :title="t('chat.call.reject')"
        :disabled="callStore.working"
        @click="handleReject"
      >
        <Icon icon="lucide:phone-off" />
      </XhButton>
      <XhButton
        tone="success"
        variant="solid"
        size="sm"
        icon-only
        :aria-label="t('chat.call.accept')"
        :title="t('chat.call.accept')"
        :disabled="callStore.working"
        @click="handleAccept"
      >
        <Icon :icon="isVideo ? 'lucide:video' : 'lucide:phone'" />
      </XhButton>
    </div>
  </Transition>

  <!-- 通话窗：开合只随通话走，没有关闭钮（要结束就挂断） -->
  <XhFloatingPanelRoot
    v-model:position="position"
    v-model:dimensions="dimensions"
    v-model:window-state="windowState"
    :open="windowOpen"
    :min-size="MIN_SIZE"
  >
    <XhFloatingPanelPositioner>
      <XhFloatingPanelContent>
        <XhFloatingPanelHeader>
          <XhFloatingPanelTitle>{{ t('chat.call.window_title', { media: mediaLabel, name: peerName }) }}</XhFloatingPanelTitle>
          <!-- 收拢成标题栏时正文藏着，状态与时长挪到这里；每秒在变，不交给读屏播报 -->
          <span v-if="windowState === 'minimized'" class="shrink-0 text-xs tabular-nums text-muted-foreground" aria-hidden="true">{{ statusText }}</span>
          <XhFloatingPanelDragTrigger />
          <XhFloatingPanelWindowStateTrigger window-state="minimized" />
          <XhFloatingPanelWindowStateTrigger window-state="maximized" />
        </XhFloatingPanelHeader>
        <XhFloatingPanelBody class="app-chat-call__body">
          <!-- 对端的声音：独立的音频元素，画面关着、窗口收拢时照样出声 -->
          <audio ref="remoteAudio" autoplay />

          <div class="app-chat-call__stage" :class="{ 'app-chat-call__stage--video': showRemoteVideo }">
            <video
              v-show="showRemoteVideo"
              ref="remoteVideo"
              class="app-chat-call__remote"
              autoplay
              playsinline
              muted
              :aria-label="t('chat.call.remote_video', { name: peerName })"
            />
            <div v-if="!showRemoteVideo" class="flex flex-col items-center gap-2 text-center">
              <XUserAvatar :avatar="callStore.peer?.avatarFileId ?? null" :name="peerName" :size="72" />
              <div class="max-w-full truncate text-sm font-semibold text-foreground">
                {{ peerName }}
              </div>
            </div>

            <!-- 状态一行：呼叫中、连接中，连上后是时长
                 读屏只播报状态的变化；时长每秒在变，标成计时器、不逐秒播报 -->
            <div class="app-chat-call__status">
              <span v-if="callStore.phase === 'connected'" role="timer" class="tabular-nums">{{ statusText }}</span>
              <span v-else role="status">{{ statusText }}</span>
              <span v-if="callStore.phase === 'connected' && !callStore.peerMicOn" role="status" class="inline-flex items-center gap-1">
                <Icon icon="lucide:mic-off" width="12" height="12" />
                {{ t('chat.call.peer_muted') }}
              </span>
            </div>

            <video
              v-show="showLocalVideo"
              ref="localVideo"
              class="app-chat-call__local"
              autoplay
              playsinline
              muted
              :aria-label="t('chat.call.local_video')"
            />
          </div>

          <div class="app-chat-call__controls">
            <XIconButton
              :icon="callStore.micOn ? 'lucide:mic' : 'lucide:mic-off'"
              :label="callStore.micOn ? t('chat.call.mute') : t('chat.call.unmute')"
              :variant="callStore.micOn ? 'ghost' : 'subtle'"
              :aria-pressed="!callStore.micOn"
              @click="callStore.setMicOn(!callStore.micOn)"
            />
            <XIconButton
              v-if="isVideo"
              :icon="callStore.cameraOn ? 'lucide:video' : 'lucide:video-off'"
              :label="callStore.cameraOn ? t('chat.call.camera_off') : t('chat.call.camera_on')"
              :variant="callStore.cameraOn ? 'ghost' : 'subtle'"
              :aria-pressed="!callStore.cameraOn"
              @click="callStore.setCameraOn(!callStore.cameraOn)"
            />
            <XhPopoverRoot v-model:open="devicesOpen" placement="top">
              <XhPopoverTrigger as-child>
                <XhButton variant="ghost" size="sm" icon-only class="x-icon-btn" :aria-label="t('chat.call.devices')" :title="t('chat.call.devices')">
                  <Icon icon="lucide:settings-2" />
                </XhButton>
              </XhPopoverTrigger>
              <XhPopoverPositioner>
                <XhPopoverContent :aria-label="t('chat.call.devices')">
                  <div class="flex w-64 flex-col gap-2">
                    <label class="flex flex-col gap-1 text-xs text-muted-foreground">
                      {{ t('chat.call.microphone') }}
                      <XSelect
                        :value="microphoneId"
                        :options="microphones"
                        :aria-label="t('chat.call.microphone')"
                        @update:value="value => handleSwitchDevice('audioinput', value)"
                      />
                    </label>
                    <label v-if="isVideo" class="flex flex-col gap-1 text-xs text-muted-foreground">
                      {{ t('chat.call.camera') }}
                      <XSelect
                        :value="cameraId"
                        :options="cameras"
                        :aria-label="t('chat.call.camera')"
                        @update:value="value => handleSwitchDevice('videoinput', value)"
                      />
                    </label>
                    <label v-if="speakers.length > 0" class="flex flex-col gap-1 text-xs text-muted-foreground">
                      {{ t('chat.call.speaker') }}
                      <XSelect
                        :value="speakerId"
                        :options="speakers"
                        :placeholder="t('chat.call.speaker_default')"
                        :aria-label="t('chat.call.speaker')"
                        @update:value="handleSwitchSpeaker"
                      />
                    </label>
                  </div>
                </XhPopoverContent>
              </XhPopoverPositioner>
            </XhPopoverRoot>
            <XhButton tone="danger" variant="solid" size="sm" @click="handleHangup">
              <Icon icon="lucide:phone-off" />
              {{ callStore.phase === 'outgoing' ? t('chat.call.cancel') : t('chat.call.hangup') }}
            </XhButton>
          </div>
        </XhFloatingPanelBody>
      </XhFloatingPanelContent>
    </XhFloatingPanelPositioner>
  </XhFloatingPanelRoot>
</template>

<style scoped>
/* 来电卡片：实体底、描边与柔和投影，停在视口右上、顶栏与标签栏之下，盖过页面与对话框 */
.app-chat-call__incoming {
  position: fixed;
  inset-block-start: calc(var(--xh-space-8) * 3);
  inset-inline-end: var(--xh-space-4);
  z-index: var(--xh-z-toast);
  display: flex;
  gap: var(--xh-space-3);
  align-items: center;
  inline-size: min(calc(var(--xh-space-8) * 10), calc(100vw - var(--xh-space-8)));
  padding: var(--xh-space-3);
  border: 1px solid var(--xh-border-default);
  border-radius: var(--xh-shape-sheet);
  background: var(--xh-bg-surface);
  box-shadow: var(--xh-elevation-floating);
}

/* 正文自己分上下两段：画面占满余下的高度，控制条贴底
   收拢时组件库给正文打 hidden，这里避开它，免得本条压过组件库的隐藏 */
.app-chat-call__body:not([hidden]) {
  display: flex;
  flex-direction: column;
  padding: 0;
  overflow: hidden;
}

.app-chat-call__stage {
  position: relative;
  display: flex;
  flex: 1;
  flex-direction: column;
  gap: var(--xh-space-3);
  align-items: center;
  justify-content: center;
  min-block-size: 0;
  padding: var(--xh-space-4);
  background: var(--xh-bg-muted);
  overflow: hidden;
}

/* 有对端的画面时状态行靠上，不挡在画面正中 */
.app-chat-call__stage--video {
  justify-content: flex-start;
}

.app-chat-call__remote {
  position: absolute;
  inset: 0;
  inline-size: 100%;
  block-size: 100%;
  object-fit: cover;
}

/* 状态行：有画面时压在画面上，垫一层实体底保证看得清 */
.app-chat-call__status {
  position: relative;
  display: flex;
  gap: var(--xh-space-2);
  align-items: center;
  padding: var(--xh-space-0_5) var(--xh-space-2);
  border-radius: var(--xh-shape-pill);
  background: var(--xh-bg-surface);
  color: var(--xh-fg-muted);
  font-size: var(--xh-font-size-xs);
}

/* 自己的画面：右下角的小窗，左右镜像（照镜子的方向） */
.app-chat-call__local {
  position: absolute;
  inset-block-end: var(--xh-space-3);
  inset-inline-end: var(--xh-space-3);
  inline-size: 28%;
  aspect-ratio: 4 / 3;
  border: 1px solid var(--xh-border-default);
  border-radius: var(--xh-shape-control);
  background: var(--xh-bg-surface);
  object-fit: cover;
  transform: scaleX(-1);
}

.app-chat-call__controls {
  display: flex;
  flex-shrink: 0;
  gap: var(--xh-space-2);
  align-items: center;
  justify-content: center;
  padding: var(--xh-space-2_5) var(--xh-space-3);
  border-block-start: 1px solid var(--xh-border-default);
  background: var(--xh-bg-surface);
}
</style>
