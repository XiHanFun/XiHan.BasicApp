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
import { useUserStore } from '~/stores'
import { callMediaFailureText, callMediaLabel, callParticipantName, formatCallDuration, isGroupCall } from '../call/call-helpers'
import { canShareScreen, ChatCallMediaError, listCallDevices } from '../call/call-media'
import { useChatCallStore } from '../call/call-store'
import { chatErrorText } from '../components/chat-helpers'
import ChatCallAudio from './ChatCallAudio.vue'
import ChatCallTile from './ChatCallTile.vue'

/**
 * 通话的全局界面：来电卡片与通话窗，挂在布局根部，任何页面都能接听，离开聊天页通话不断
 *
 * 来电卡片在每个开着的页签都出现；通话窗只在占着这通电话的页签（发起了、接听了或加入了的那个）出现，
 * 非模态、可拖动、可收拢成标题栏或铺满视口，通话期间照常用页面
 * 通话窗里一人一格（还在响铃的也占一格），有人共享屏幕时屏幕占主画面、各人收成底下一排
 */
defineOptions({ name: 'AppChatCall' })

/** 通话窗与视口右缘、上缘的留白（px） */
const GAP = 16
/** 顶栏与标签栏下方再落位，不压住它们的按钮（px） */
const TOP = 96
const VOICE_SIZE: FloatingPanelSize = { width: 320, height: 380 }
const VIDEO_SIZE: FloatingPanelSize = { width: 520, height: 440 }
/** 多人通话与看共享的屏幕要大一些 */
const GROUP_SIZE: FloatingPanelSize = { width: 600, height: 480 }
const MIN_SIZE: FloatingPanelSize = { width: 280, height: 300 }

const { t } = useI18n()
const callStore = useChatCallStore()
const userStore = useUserStore()
const { isMobile } = useIsMobile()

const call = computed(() => callStore.call)
const isVideo = computed(() => call.value?.media === 'Video')
const isGroup = computed(() => (call.value
  ? isGroupCall(call.value)
  : false))
/** 一对一是对方，多人通话是发起人（来电卡片上说是谁叫的） */
const peerName = computed(() => callParticipantName(callStore.peer) || t('chat.call.unknown_peer'))
const groupName = computed(() => call.value?.conversationName?.trim() || t('chat.call.group_fallback'))
const mediaLabel = computed(() => callMediaLabel(t, call.value?.media ?? 'Voice'))
const incoming = computed(() => callStore.phase === 'incoming')
/** 占着这通电话的页签才开通话窗 */
const windowOpen = computed(() => callStore.engaged && call.value !== null)

const incomingDetail = computed(() => {
  if (!isGroup.value) {
    return isVideo.value
      ? t('chat.call.incoming_video')
      : t('chat.call.incoming_voice')
  }
  return isVideo.value
    ? t('chat.call.incoming_group_video', { group: groupName.value })
    : t('chat.call.incoming_group_voice', { group: groupName.value })
})

// ── 一人一格：已经在里面的与还在响铃的 ───────────────────────────────
interface CallTile {
  userId: string
  name: string
  avatar: null | string
  /** 看得到的画面：视频通话、他开着摄像头、轨道已经到了 */
  video: MediaStream | null
  status?: string
  mutedLabel?: string
  dimmed: boolean
}

const tiles = computed<CallTile[]>(() => {
  const current = call.value
  if (!current) {
    return []
  }
  const mine = userStore.userInfo?.basicId ?? ''
  const joined = new Map(callStore.peers.map(peer => [peer.userId, peer]))
  return current.participants
    .filter(participant => participant.userId !== mine && (participant.status === 'Joined' || participant.status === 'Invited'))
    .map((participant) => {
      const name = callParticipantName(participant) || t('chat.call.unknown_peer')
      const tile = { userId: participant.userId, name, avatar: participant.avatarFileId ?? null }
      if (participant.status === 'Invited') {
        return {
          ...tile,
          video: null,
          status: t('chat.call.tile_ringing'),
          dimmed: true,
        }
      }
      const peer = joined.get(participant.userId)
      if (!peer || peer.connection === 'connecting') {
        return {
          ...tile,
          video: null,
          status: t('chat.call.tile_connecting'),
          dimmed: false,
        }
      }
      if (peer.connection === 'failed') {
        return {
          ...tile,
          video: null,
          status: t('chat.call.tile_failed'),
          dimmed: true,
        }
      }
      return {
        ...tile,
        video: isVideo.value && peer.cameraOn
          ? peer.camera
          : null,
        mutedLabel: peer.micOn
          ? undefined
          : t('chat.call.tile_muted', { name }),
        dimmed: false,
      }
    })
})

/** 主画面上那块共享的屏幕：先看别人的，没有别人在共享才看自己的 */
const sharedScreen = computed(() => {
  const sharer = callStore.peers.find(peer => peer.screenOn && peer.screen !== null)
  if (sharer) {
    return {
      stream: sharer.screen,
      label: t('chat.call.screen_of', { name: callParticipantName(sharer.participant) || t('chat.call.unknown_peer') }),
    }
  }
  return callStore.screenStream
    ? {
        stream: callStore.screenStream,
        label: t('chat.call.screen_mine'),
      }
    : null
})

const windowTitle = computed(() => (isGroup.value
  ? t('chat.call.window_title_group', { media: mediaLabel.value, name: groupName.value, n: callStore.peers.length + 1 })
  : t('chat.call.window_title', { media: mediaLabel.value, name: peerName.value })))

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
    case 'waiting':
      return t('chat.call.status_waiting')
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
  const preferred = isGroup.value
    ? GROUP_SIZE
    : isVideo.value
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

// 有人开始共享屏幕：窗口太小看不清，撑到看屏幕的大小（用户自己收拢或铺满了的不动）
watch(() => sharedScreen.value !== null, (sharing) => {
  if (!sharing || windowState.value !== 'default') {
    return
  }
  const width = Math.max(dimensions.value.width, Math.min(GROUP_SIZE.width, window.innerWidth - GAP * 2))
  const height = Math.max(dimensions.value.height, Math.min(GROUP_SIZE.height, window.innerHeight - TOP - GAP))
  dimensions.value = { width, height }
  position.value = {
    x: Math.max(GAP, Math.min(position.value.x, window.innerWidth - width - GAP)),
    y: Math.max(GAP, Math.min(position.value.y, window.innerHeight - height - GAP)),
  }
})

// ── 自己的画面：右下角的小窗 ────────────────────────────────────────
const localVideo = ref<HTMLVideoElement | null>(null)

watch([
  localVideo,
  () => callStore.localStream,
], ([element, stream]) => {
  if (element && element.srcObject !== stream) {
    element.srcObject = stream
  }
}, { flush: 'post' })

const showLocalVideo = computed(() => isVideo.value && callStore.cameraOn && callStore.localStream !== null)
/** 一对一时自己的画面是右下角的小窗；多人或有屏幕在共享时进宫格占一格，不压在别人的格子上 */
const localInGrid = computed(() => showLocalVideo.value && (tiles.value.length > 1 || sharedScreen.value !== null))

/** 宫格的列数：人越多分得越细 */
const columns = computed(() => {
  const count = tiles.value.length + (localInGrid.value
    ? 1
    : 0)
  if (count <= 1) {
    return 1
  }
  if (count <= 4) {
    return 2
  }
  return count <= 9
    ? 3
    : 4
})

const avatarSize = computed(() => {
  if (tiles.value.length <= 1) {
    return 72
  }
  return tiles.value.length <= 4
    ? 56
    : 44
})

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

function handleSwitchSpeaker(value: unknown): void {
  if (typeof value === 'string' && value !== '') {
    speakerId.value = value
  }
}

/** 换不到选的扬声器：退回系统缺省；每个人的音频元素各报一次，只提示一次 */
function handleSpeakerError(error: unknown): void {
  if (speakerId.value !== null) {
    speakerId.value = null
    toast.danger(mediaErrorText(error))
  }
}

// ── 屏幕共享 ────────────────────────────────────────────────────────
const screenShareable = canShareScreen()

async function handleToggleScreen(): Promise<void> {
  if (callStore.screenOn) {
    callStore.stopScreenShare()
    return
  }
  try {
    await callStore.startScreenShare()
  }
  catch {
    toast.danger(t('chat.call.screen_failed'))
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
/** 多人通话不点某一个人的名（取消的除外：那是发起人取消的） */
const GROUP_NOTICE_KEYS: Partial<Record<ChatCallNoticeKind, string>> = {
  'no-answer': 'chat.call.notice_group_no_answer',
  'interrupted': 'chat.call.notice_group_interrupted',
  'connect-failed': 'chat.call.notice_group_connect_failed',
}

watch(() => callStore.notice, (notice) => {
  if (!notice) {
    return
  }
  const key = (notice.group
    ? GROUP_NOTICE_KEYS[notice.kind]
    : undefined) ?? NOTICE_KEYS[notice.kind]
  const text = t(key, { name: notice.peerName || t('chat.call.unknown_peer') })
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
          {{ incomingDetail }}
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
          <XhFloatingPanelTitle>{{ windowTitle }}</XhFloatingPanelTitle>
          <!-- 收拢成标题栏时正文藏着，状态与时长挪到这里；每秒在变，不交给读屏播报 -->
          <span v-if="windowState === 'minimized'" class="shrink-0 text-xs tabular-nums text-muted-foreground" aria-hidden="true">{{ statusText }}</span>
          <XhFloatingPanelDragTrigger />
          <XhFloatingPanelWindowStateTrigger window-state="minimized" />
          <XhFloatingPanelWindowStateTrigger window-state="maximized" />
        </XhFloatingPanelHeader>
        <XhFloatingPanelBody class="app-chat-call__body">
          <!-- 各人的声音：每人一个音频元素，画面关着、窗口收拢时照样出声 -->
          <ChatCallAudio
            v-for="peer in callStore.peers"
            :key="peer.userId"
            :stream="peer.audio"
            :sink-id="speakerId"
            @sink-error="handleSpeakerError"
          />

          <div class="app-chat-call__stage">
            <!-- 共享的屏幕占主画面，各人收成底下一排 -->
            <ChatCallTile
              v-if="sharedScreen"
              class="app-chat-call__screen"
              fit="contain"
              :name="sharedScreen.label"
              :stream="sharedScreen.stream"
              :video-label="sharedScreen.label"
            />
            <div
              v-if="tiles.length > 0 || localInGrid"
              class="app-chat-call__grid"
              :class="{ 'app-chat-call__grid--strip': sharedScreen !== null }"
              :style="{ '--app-chat-call-columns': columns }"
            >
              <ChatCallTile
                v-for="tile in tiles"
                :key="tile.userId"
                :name="tile.name"
                :avatar="tile.avatar"
                :avatar-size="sharedScreen ? 44 : avatarSize"
                :stream="tile.video"
                :video-label="t('chat.call.remote_video', { name: tile.name })"
                :status="tile.status"
                :muted-label="tile.mutedLabel"
                :dimmed="tile.dimmed"
              />
              <ChatCallTile
                v-if="localInGrid"
                mirrored
                :name="t('chat.call.local_video')"
                :stream="callStore.localStream"
                :video-label="t('chat.call.local_video')"
              />
            </div>

            <!-- 状态一行：呼叫中、连接中、等人，连上后是时长
                 读屏只播报状态的变化；时长每秒在变，标成计时器、不逐秒播报 -->
            <div class="app-chat-call__status-row">
              <div class="app-chat-call__status">
                <span v-if="callStore.phase === 'connected'" role="timer" class="tabular-nums">{{ statusText }}</span>
                <span v-else role="status">{{ statusText }}</span>
              </div>
            </div>

            <video
              v-show="showLocalVideo && !localInGrid"
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
            <XIconButton
              v-if="screenShareable"
              :icon="callStore.screenOn ? 'lucide:screen-share-off' : 'lucide:screen-share'"
              :label="callStore.screenOn ? t('chat.call.stop_share') : t('chat.call.share_screen')"
              :variant="callStore.screenOn ? 'subtle' : 'ghost'"
              :aria-pressed="callStore.screenOn"
              :disabled="call?.status !== 'Ongoing'"
              @click="handleToggleScreen"
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
  gap: var(--xh-space-2);
  min-block-size: 0;
  padding: var(--xh-space-2);
  background: var(--xh-bg-muted);
  overflow: hidden;
}

/* 共享的屏幕：占满余下的高度 */
.app-chat-call__screen {
  flex: 1;
}

/* 宫格：列数随人数给，行高均分，一人一格 */
.app-chat-call__grid {
  display: grid;
  flex: 1;
  grid-template-columns: repeat(var(--app-chat-call-columns, 1), minmax(0, 1fr));
  grid-auto-rows: minmax(0, 1fr);
  gap: var(--xh-space-2);
  min-block-size: 0;
}

/* 有屏幕在共享时各人收成底下一排：定高、等宽，多了横向滚 */
.app-chat-call__grid--strip {
  display: flex;
  flex: none;
  block-size: calc(var(--xh-space-8) * 3);
  overflow-x: auto;
}

.app-chat-call__grid--strip > * {
  flex: none;
  aspect-ratio: 4 / 3;
}

/* 状态行：浮在画面上沿正中，不拦下面的点击 */
.app-chat-call__status-row {
  position: absolute;
  inset-block-start: var(--xh-space-3);
  inset-inline: 0;
  display: flex;
  justify-content: center;
  pointer-events: none;
}

/* 垫一层实体底，压在画面上也看得清 */
.app-chat-call__status {
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
