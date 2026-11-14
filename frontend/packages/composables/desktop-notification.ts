/*
 * Copyright (c) 2021-Present XiHanFun and contributors.
 * Licensed under the MIT License. See LICENSE in the project root for license information.
 */

/**
 * 桌面通知（浏览器的系统通知）
 *
 * 页面不在眼前（标签页被盖住或窗口没有焦点）时用系统通知提醒；页面在前台时站内的提示已经看得到，不再发
 * 授权只能在用户手势里申请：打开开关时问，推送到达时不问；没有授权即不发
 */

/** 浏览器对通知的授权状态；unsupported 是这个环境没有通知能力 */
export type DesktopNotificationPermission = 'default' | 'denied' | 'granted' | 'unsupported'

/** 一条桌面通知 */
export interface DesktopNotificationOptions {
  title: string
  body?: string
  /** 同一标签的通知互相顶替：同一个会话只留最新一条 */
  tag?: string
  /** 点了通知之后做什么；窗口先被拉回前台 */
  onClick?: () => void
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

/** 页面在不在眼前：标签页可见且窗口有焦点 */
function pageInForeground(): boolean {
  return document.visibilityState === 'visible' && document.hasFocus()
}

/**
 * 发一条桌面通知
 * 页面在前台、没有授权或环境不支持时不发；点通知把窗口拉回前台再交给 onClick
 * @returns 发出去了没有
 */
export function showDesktopNotification(options: DesktopNotificationOptions): boolean {
  if (desktopNotificationPermission() !== 'granted' || pageInForeground())
    return false
  let notification: Notification
  try {
    notification = new Notification(options.title, { body: options.body, tag: options.tag })
  }
  catch {
    // 有的环境（如移动端 Chrome）授权了也只许 Service Worker 发通知，构造即抛：当作这里发不了
    return false
  }
  notification.onclick = () => {
    window.focus()
    notification.close()
    options.onClick?.()
  }
  return true
}
