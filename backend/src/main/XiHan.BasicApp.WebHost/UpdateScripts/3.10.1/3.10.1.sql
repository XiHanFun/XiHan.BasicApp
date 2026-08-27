-- 3.10.1
-- 移除用户表的时区与语言列。
--
-- 这两列自始至终没有被任何行为消费：后端的时间换算读的是请求头 X-Timezone，
-- 界面语言取的是前端本地语言偏好，二者都随「偏好设置」跨设备同步（sys_user_setting）。
-- 个人中心里那两个字段只是把值写进本表，改了不生效、改别处又不回写，属于第二份事实源，
-- 故连同前端字段一并移除。
--
-- 标识符一律小写不加引号：SqlSugar 建表未加引号，PostgreSQL 折叠为小写，
-- 实际列名是 time_zone / language 而非实体上声明的 TimeZone / Language。

ALTER TABLE sys_user DROP COLUMN IF EXISTS time_zone;
ALTER TABLE sys_user DROP COLUMN IF EXISTS language;
