// 由 scripts/gen-contracts.mjs 从 contracts/realtime.json 生成；不要手改，改后端契约后执行 pnpm gen:contracts。

/** 实时 Hub 的路径（后端 RealtimeHubs）。 */
export const RealtimeHubs = {
  Notification: '/hubs/notification',
} as const

/** 服务端推送的方法名（后端 RealtimeMethods）：订阅一律用这里的常量，后端改名时生成物漂移、类型检查报错。 */
export const RealtimeMethods = {
  ForceLogout: 'ForceLogout',
  ReceiveNotification: 'ReceiveNotification',
  TaskProgress: 'TaskProgress',
  UserSettingChanged: 'UserSettingChanged',
} as const

export type RealtimeMethod = typeof RealtimeMethods[keyof typeof RealtimeMethods]
