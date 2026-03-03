// ==================== 性别 ====================

export const GENDER_OPTIONS = [
  { label: '未知', value: 0 },
  { label: '男', value: 1 },
  { label: '女', value: 2 },
]

// ==================== 状态 ====================

export const STATUS_OPTIONS = [
  { label: '启用', value: 1 },
  { label: '禁用', value: 0 },
]

// ==================== 菜单类型 ====================

export const MENU_TYPE = {
  DIR: 0,
  MENU: 1,
  BUTTON: 2,
} as const

// ==================== 文件 ====================

export const FILE_TYPE_OPTIONS = [
  { label: '图片', value: 0 },
  { label: '文档', value: 1 },
  { label: '视频', value: 2 },
  { label: '音频', value: 3 },
  { label: '压缩包', value: 4 },
  { label: '其他', value: 99 },
]

export const FILE_STATUS_OPTIONS = [
  { label: '正常', value: 0 },
  { label: '上传中', value: 1 },
  { label: '上传失败', value: 2 },
  { label: '处理中', value: 3 },
  { label: '已删除', value: 4 },
  { label: '已归档', value: 5 },
  { label: '已过期', value: 6 },
  { label: '损坏', value: 7 },
  { label: '违规', value: 8 },
]

// ==================== 邮件 ====================

export const EMAIL_TYPE_OPTIONS = [
  { label: '系统邮件', value: 0 },
  { label: '验证邮件', value: 1 },
  { label: '通知邮件', value: 2 },
  { label: '营销邮件', value: 3 },
  { label: '自定义邮件', value: 99 },
]

export const EMAIL_STATUS_OPTIONS = [
  { label: '待发送', value: 0 },
  { label: '发送中', value: 1 },
  { label: '发送成功', value: 2 },
  { label: '发送失败', value: 3 },
  { label: '已取消', value: 4 },
]

// ==================== 短信 ====================

export const SMS_TYPE_OPTIONS = [
  { label: '验证码', value: 0 },
  { label: '通知短信', value: 1 },
  { label: '营销短信', value: 2 },
  { label: '自定义短信', value: 99 },
]

export const SMS_STATUS_OPTIONS = [
  { label: '待发送', value: 0 },
  { label: '发送中', value: 1 },
  { label: '发送成功', value: 2 },
  { label: '发送失败', value: 3 },
  { label: '已取消', value: 4 },
]

// ==================== 任务 ====================

export const TRIGGER_TYPE_OPTIONS = [
  { label: '立即执行', value: 0 },
  { label: '定时执行', value: 1 },
  { label: '循环执行', value: 2 },
  { label: 'Cron 表达式', value: 3 },
]

export const RUN_TASK_STATUS_OPTIONS = [
  { label: '待执行', value: 0 },
  { label: '执行中', value: 1 },
  { label: '执行成功', value: 2 },
  { label: '执行失败', value: 3 },
  { label: '已停止', value: 4 },
  { label: '已暂停', value: 5 },
]

// ==================== OAuth 应用 ====================

export const OAUTH_APP_TYPE_OPTIONS = [
  { label: 'Web 应用', value: 0 },
  { label: '移动应用', value: 1 },
  { label: '桌面应用', value: 2 },
  { label: '服务应用', value: 3 },
]

// ==================== 审查 ====================

export const REVIEW_STATUS_OPTIONS = [
  { label: '待审查', value: 0 },
  { label: '审查中', value: 1 },
  { label: '已通过', value: 2 },
  { label: '已拒绝', value: 3 },
  { label: '已撤回', value: 4 },
]

export const REVIEW_RESULT_OPTIONS = [
  { label: '通过', value: 0 },
  { label: '拒绝', value: 1 },
  { label: '退回修改', value: 2 },
]

// ==================== 会话 ====================

export const DEVICE_TYPE_OPTIONS = [
  { label: '未知', value: 0 },
  { label: 'Web 浏览器', value: 1 },
  { label: 'iOS', value: 2 },
  { label: 'Android', value: 3 },
  { label: 'Windows', value: 4 },
  { label: 'macOS', value: 5 },
  { label: 'Linux', value: 6 },
  { label: '平板', value: 7 },
  { label: '小程序', value: 8 },
  { label: 'API 调用', value: 9 },
]

// ==================== 配置 ====================

export const CONFIG_TYPE_OPTIONS = [
  { label: '系统配置', value: 0 },
  { label: '用户配置', value: 1 },
  { label: '应用配置', value: 2 },
]

export const CONFIG_DATA_TYPE_OPTIONS = [
  { label: '字符串', value: 0 },
  { label: '数字', value: 1 },
  { label: '布尔值', value: 2 },
  { label: 'JSON对象', value: 3 },
  { label: '数组', value: 4 },
]

// ==================== 通知 ====================

export const NOTIFICATION_TYPE_OPTIONS = [
  { label: '系统通知', value: 0 },
  { label: '用户通知', value: 1 },
  { label: '公告', value: 2 },
  { label: '警告', value: 3 },
  { label: '错误', value: 4 },
]

export const NOTIFICATION_STATUS_OPTIONS = [
  { label: '未读', value: 0 },
  { label: '已读', value: 1 },
  { label: '已删除', value: 2 },
]
