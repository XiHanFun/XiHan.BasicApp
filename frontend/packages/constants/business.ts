// ==================== 性别 ====================

export const GENDER_OPTIONS = [
  { label: '未知', value: 0 },
  { label: '男', value: 1 },
  { label: '女', value: 2 },
]

// ==================== 通用状态 ====================

export const STATUS_OPTIONS = [
  { label: '启用', value: 1 },
  { label: '禁用', value: 0 },
]

export const VALIDITY_STATUS_OPTIONS = [
  { label: '无效', value: 0 },
  { label: '有效', value: 1 },
]

// ==================== 部门 ====================

export const DEPARTMENT_TYPE_OPTIONS = [
  { label: '集团', value: 0 },
  { label: '总部', value: 1 },
  { label: '公司', value: 2 },
  { label: '分公司', value: 3 },
  { label: '事业部', value: 4 },
  { label: '中心', value: 5 },
  { label: '部门', value: 6 },
  { label: '科室', value: 7 },
  { label: '团队', value: 8 },
  { label: '小组', value: 9 },
  { label: '项目组', value: 10 },
  { label: '工作组', value: 11 },
  { label: '虚拟组织', value: 12 },
  { label: '办事处', value: 13 },
  { label: '子公司', value: 14 },
  { label: '其他', value: 99 },
]

// ==================== 角色 ====================

export const ROLE_TYPE_OPTIONS = [
  { label: '系统', value: 0 },
  { label: '业务', value: 1 },
  { label: '自定义', value: 2 },
]

export const DATA_SCOPE_OPTIONS = [
  { label: '本人数据', value: 0 },
  { label: '本部门', value: 1 },
  { label: '本部门及下级', value: 2 },
  { label: '全部数据', value: 3 },
  { label: '自定义', value: 99 },
]

export const PERMISSION_ACTION_OPTIONS = [
  { label: '授予权限', value: 0 },
  { label: '拒绝权限', value: 1 },
]

// ==================== 租户 ====================

export const TENANT_ISOLATION_MODE_OPTIONS = [
  { label: '字段隔离', value: 0 },
  { label: '数据库隔离', value: 1 },
  { label: 'Schema隔离', value: 2 },
]

export const TENANT_STATUS_OPTIONS = [
  { label: '正常', value: 0 },
  { label: '暂停', value: 1 },
  { label: '过期', value: 2 },
  { label: '禁用', value: 3 },
]

export const TENANT_CONFIG_STATUS_OPTIONS = [
  { label: '待配置', value: 0 },
  { label: '配置中', value: 1 },
  { label: '已配置', value: 2 },
  { label: '配置失败', value: 3 },
  { label: '已停用', value: 4 },
]

export const MEMBER_TYPE_OPTIONS = [
  { label: '租户所有者', value: 0 },
  { label: '租户管理员', value: 1 },
  { label: '普通成员', value: 2 },
  { label: '外部协作者', value: 3 },
  { label: '访客', value: 4 },
  { label: '顾问', value: 5 },
  { label: '平台管理员', value: 99 },
]

export const MEMBER_INVITE_STATUS_OPTIONS = [
  { label: '待接受', value: 0 },
  { label: '已接受', value: 1 },
  { label: '已拒绝', value: 2 },
  { label: '已撤销', value: 3 },
  { label: '已过期', value: 4 },
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

export const OPENAPI_SIGNATURE_ALGORITHM_OPTIONS = [
  { label: 'HMACSHA256', value: 'HMACSHA256' },
  { label: 'HMACSHA512', value: 'HMACSHA512' },
  { label: 'HMACSHA1', value: 'HMACSHA1' },
  { label: 'RSASHA256', value: 'RSASHA256' },
  { label: 'SM2', value: 'SM2' },
]

export const OPENAPI_CONTENT_SIGN_ALGORITHM_OPTIONS = [
  { label: 'SHA256', value: 'SHA256' },
  { label: 'SHA512', value: 'SHA512' },
  { label: 'MD5', value: 'MD5' },
]

export const OPENAPI_ENCRYPT_ALGORITHM_OPTIONS = [
  { label: 'AES-CBC', value: 'AES-CBC' },
  { label: 'BLOWFISH', value: 'BLOWFISH' },
  { label: 'NONE', value: 'NONE' },
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

// ==================== 会话/设备 ====================

export const DEVICE_TYPE_OPTIONS = [
  { label: '未知', value: 0 },
  { label: 'Web浏览器', value: 1 },
  { label: 'iOS移动端', value: 2 },
  { label: 'Android移动端', value: 3 },
  { label: 'Windows桌面', value: 4 },
  { label: 'macOS桌面', value: 5 },
  { label: 'Linux桌面', value: 6 },
  { label: '平板设备', value: 7 },
  { label: '小程序', value: 8 },
  { label: 'API调用', value: 9 },
]

export const TWO_FACTOR_METHOD_OPTIONS = [
  { label: '未启用', value: 0 },
  { label: 'TOTP', value: 1 },
  { label: '邮箱验证码', value: 2 },
  { label: '短信验证码', value: 4 },
]

export const STATISTICS_PERIOD_OPTIONS = [
  { label: '今日', value: 0 },
  { label: '昨日', value: 1 },
  { label: '本周', value: 2 },
  { label: '上周', value: 3 },
  { label: '本月', value: 4 },
  { label: '上月', value: 5 },
  { label: '本年', value: 6 },
  { label: '去年', value: 7 },
  { label: '自定义', value: 99 },
]

// ==================== 配置 ====================

export const CONFIG_TYPE_OPTIONS = [
  { label: '系统配置', value: 0 },
  { label: '用户配置', value: 1 },
  { label: '应用配置', value: 2 },
  { label: '业务配置', value: 3 },
]

export const CONFIG_DATA_TYPE_OPTIONS = [
  { label: '字符串', value: 0 },
  { label: '数字', value: 1 },
  { label: '布尔值', value: 2 },
  { label: 'JSON对象', value: 3 },
  { label: '数组', value: 4 },
]

// ==================== 权限 ====================

export const PERMISSION_TYPE_OPTIONS = [
  { label: '资源操作', value: 0 },
  { label: '功能', value: 1 },
  { label: '数据范围', value: 2 },
]

// ==================== 资源 ====================

export const RESOURCE_TYPE_OPTIONS = [
  { label: 'API', value: 0 },
  { label: '文件', value: 1 },
  { label: '数据表', value: 2 },
  { label: '业务对象', value: 3 },
  { label: '其他', value: 99 },
]

export const RESOURCE_ACCESS_LEVEL_OPTIONS = [
  { label: '匿名访问', value: 0 },
  { label: '仅需认证', value: 1 },
  { label: '需要授权', value: 2 },
]

// ==================== 操作 ====================

export const HTTP_METHOD_OPTIONS = [
  { label: 'GET', value: 0 },
  { label: 'POST', value: 1 },
  { label: 'PUT', value: 2 },
  { label: 'DELETE', value: 3 },
  { label: 'PATCH', value: 4 },
  { label: 'HEAD', value: 5 },
  { label: 'OPTIONS', value: 6 },
  { label: '所有方法', value: 99 },
]

export const OPERATION_CATEGORY_OPTIONS = [
  { label: 'CRUD', value: 0 },
  { label: '业务', value: 1 },
  { label: '管理', value: 2 },
  { label: '系统', value: 3 },
  { label: '自定义', value: 99 },
]

export const OPERATION_TYPE_OPTIONS = [
  { label: '创建', value: 0 },
  { label: '读取', value: 1 },
  { label: '更新', value: 2 },
  { label: '删除', value: 3 },
  { label: '查看详情', value: 4 },
  { label: '审批', value: 10 },
  { label: '执行', value: 11 },
  { label: '导入', value: 20 },
  { label: '导出', value: 21 },
  { label: '上传', value: 22 },
  { label: '下载', value: 23 },
  { label: '打印', value: 24 },
  { label: '分享', value: 25 },
  { label: '授权', value: 30 },
  { label: '撤销', value: 31 },
  { label: '启用', value: 32 },
  { label: '禁用', value: 33 },
  { label: '自定义', value: 99 },
]

// ==================== 权限条件 ====================

export const CONDITION_OPERATOR_OPTIONS = [
  { label: '等于', value: 0 },
  { label: '不等于', value: 1 },
  { label: '大于', value: 2 },
  { label: '大于等于', value: 3 },
  { label: '小于', value: 4 },
  { label: '小于等于', value: 5 },
  { label: '包含', value: 6 },
  { label: '不包含', value: 7 },
  { label: '在集合中', value: 8 },
  { label: '不在集合中', value: 9 },
  { label: '在范围内', value: 10 },
  { label: '以…开头', value: 11 },
  { label: '以…结尾', value: 12 },
  { label: '为空', value: 13 },
  { label: '不为空', value: 14 },
]

// ==================== 权限委托 ====================

export const DELEGATION_STATUS_OPTIONS = [
  { label: '待生效', value: 0 },
  { label: '生效中', value: 1 },
  { label: '已过期', value: 2 },
  { label: '已撤销', value: 3 },
]

export const PERMISSION_REQUEST_STATUS_OPTIONS = [
  { label: '待审批', value: 0 },
  { label: '已批准', value: 1 },
  { label: '已拒绝', value: 2 },
  { label: '已撤回', value: 3 },
  { label: '已过期', value: 4 },
]

// ==================== 字段级安全 ====================

export const FIELD_MASK_STRATEGY_OPTIONS = [
  { label: '不脱敏', value: 0 },
  { label: '完全隐藏', value: 1 },
  { label: '全部星号', value: 2 },
  { label: '部分脱敏', value: 3 },
  { label: '哈希', value: 4 },
  { label: '固定替换', value: 5 },
  { label: '自定义', value: 99 },
]

export const FIELD_SECURITY_TARGET_TYPE_OPTIONS = [
  { label: '角色', value: 0 },
  { label: '用户', value: 1 },
  { label: '权限', value: 2 },
  { label: '部门', value: 3 },
]

// ==================== 权限变更记录 ====================

export const PERMISSION_CHANGE_TYPE_OPTIONS = [
  { label: '角色授予权限', value: 0 },
  { label: '角色撤销权限', value: 1 },
  { label: '用户直授权限', value: 2 },
  { label: '用户撤销直授权限', value: 3 },
  { label: '用户分配角色', value: 4 },
  { label: '用户移除角色', value: 5 },
  { label: '用户直授权限拒绝', value: 6 },
  { label: '角色权限拒绝', value: 7 },
]

// ==================== 约束规则 ====================

export const CONSTRAINT_TYPE_OPTIONS = [
  { label: '静态职责分离', value: 0 },
  { label: '动态职责分离', value: 1 },
  { label: '互斥约束', value: 2 },
  { label: '基数约束', value: 3 },
  { label: '先决条件', value: 4 },
  { label: '时间约束', value: 5 },
  { label: '位置约束', value: 6 },
  { label: '自定义约束', value: 99 },
]

export const VIOLATION_ACTION_OPTIONS = [
  { label: '拒绝', value: 0 },
  { label: '警告放行', value: 1 },
  { label: '仅记录日志', value: 2 },
  { label: '需要审批', value: 3 },
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
