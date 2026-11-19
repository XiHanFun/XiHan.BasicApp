/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

/**
 * 桌面通知（浏览器的系统通知）
 *
 * 页面看不到（窗口最小化、切到别的标签页或被别的窗口整个挡住）时用系统通知提醒；
 * 页面看得到时站内的提示就在眼前，窗口没有焦点（正在别的程序里操作）也不发
 * 授权只能在用户手势里申请：偏好开着而浏览器还没问过时，借用户在页面上的第一下操作申请；推送到达时不问，没有授权即不发
 */

/** 浏览器对通知的授权状态；unsupported 是这个环境没有通知能力 */
export type DesktopNotificationPermission = 'default' | 'denied' | 'granted' | 'unsupported'

/** 一条桌面通知 */
export interface DesktopNotificationOptions {
  title: string
  body?: string
  /** 同一标签的通知互相顶替：同一个会话只留最新一条，顶替的那条照样弹出 */
  tag?: string
  /** 一直挂着等用户处理、不自己消失：来电 */
  requireInteraction?: boolean
  /** 点了通知之后做什么；窗口先被拉回前台 */
  onClick?: () => void
}

/** 发出去的一条桌面通知 */
export interface DesktopNotificationHandle {
  /** 收起：它提醒的事已经有了结果（来电接了、挂了） */
  close: () => void
}

/** 通知规范里有、TypeScript 的 DOM 类型没收的选项 */
interface NotificationInit extends NotificationOptions {
  /** 同标签顶替时也重新提醒；不写即静默替换 */
  renotify?: boolean
}

/** 当前的授权状态 */
export function desktopNotificationPermission(): DesktopNotificationPermission {
  return typeof Notification === 'undefined'
    ? 'unsupported'
    : Notification.permission
}

/**
 * 申请通知授权，须在用户手势里调用；已经有结论（允许或拒绝）时不再询问
 * @returns 申请之后的授权状态
 */
export async function requestDesktopNotificationPermission(): Promise<DesktopNotificationPermission> {
  if (typeof Notification === 'undefined')
    return 'unsupported'
  if (Notification.permission !== 'default')
    return Notification.permission
  return await Notification.requestPermission()
}

/**
 * 授权状态变了就告诉一声：开关之外也会变（第一下操作时的询问、浏览器的网站设置）
 * 浏览器没有权限查询接口时什么都不做
 * @returns 取消订阅
 */
export function onDesktopNotificationPermissionChange(listener: () => void): () => void {
  let status: null | PermissionStatus = null
  let disposed = false
  navigator.permissions?.query({ name: 'notifications' })
    .then((result) => {
      if (disposed)
        return
      status = result
      status.addEventListener('change', listener)
    })
    .catch(() => {})
  return () => {
    disposed = true
    status?.removeEventListener('change', listener)
  }
}

/** 这一下按键算不算用户在操作页面：浏览器只把这类按键当成可以申请授权的手势 */
function isActivationKey(event: KeyboardEvent): boolean {
  return event.key === 'Enter' || event.key === ' ' || event.key.length === 1
}

/**
 * 浏览器还没问过授权时，借用户在页面上的第一下操作（点击或按键）申请一次
 * 偏好关着的那几下不问，等它开着；问过一次（允许、拒绝或关掉了询问）就撤掉监听，关掉询问的下次进入页面再问
 * @param wanted 偏好此刻是否开着
 * @returns 撤掉监听
 */
export function attachDesktopNotificationPrompt(wanted: () => boolean): () => void {
  if (desktopNotificationPermission() !== 'default')
    return () => {}
  function detach() {
    document.removeEventListener('click', handleClick, true)
    document.removeEventListener('keydown', handleKeydown, true)
  }
  function ask() {
    if (!wanted())
      return
    detach()
    if (desktopNotificationPermission() === 'default')
      void requestDesktopNotificationPermission()
  }
  function handleClick() {
    ask()
  }
  function handleKeydown(event: KeyboardEvent) {
    if (isActivationKey(event))
      ask()
  }
  document.addEventListener('click', handleClick, true)
  document.addEventListener('keydown', handleKeydown, true)
  return detach
}

/** 页面看不看得到：只看可见性，与窗口有没有焦点无关 */
function pageVisible(): boolean {
  return document.visibilityState === 'visible'
}

/**
 * 发一条桌面通知
 * 页面看得到、没有授权或环境不支持时不发；点通知把窗口拉回前台再交给 onClick
 * 提示音由页面按偏好自己放（消息、通知、来电各有各的音），系统不再另响一声
 * @returns 发出去的通知；没发时为 null
 */
export function showDesktopNotification(options: DesktopNotificationOptions): DesktopNotificationHandle | null {
  if (desktopNotificationPermission() !== 'granted' || pageVisible())
    return null
  const init: NotificationInit = { body: options.body, silent: true }
  if (options.tag) {
    init.tag = options.tag
    // 同标签顶替缺省是静默替换：Windows 上只改通知中心里那一条、不再弹出，用户看到的就是没来通知
    init.renotify = true
  }
  if (options.requireInteraction)
    init.requireInteraction = true
  let notification: Notification
  try {
    notification = new Notification(options.title, init)
  }
  catch {
    // 有的环境（如移动端 Chrome）授权了也只许 Service Worker 发通知，构造即抛：当作这里发不了
    return null
  }
  notification.onclick = () => {
    window.focus()
    notification.close()
    options.onClick?.()
  }
  return {
    close: () => notification.close(),
  }
}
