-- 5.2.0
-- 存量手机号码统一为 E.164（+国码+号码）。
--
-- 手机号码成为登录身份标识后，存储写法必须与登录时提交的写法一致：
-- 库里存 0912345678 而登录提交 +886912345678，比对不上且不会有任何报错。
--
-- 本脚本只处理能无歧义转换的两类：
--   1. 纯数字且以 86 开头、长度 13（中国大陆）→ 加 +
--   2. 以 0 开头的中国大陆十一位号码（0 + 1[3-9]xxxxxxxxx）→ 去 0 加 +86
-- 其余写法（含其他国家的本地号码）保持原样：默认国码因部署而异，猜错会把号码改成别人的。
-- 未转换的号码在管理后台保存用户时会被前端国码选择器要求补全。
--
-- 幂等且碰撞安全：条件里带有 not exists 检查，确保：
--   1. 未以 + 开头的值才转换（跑过一次后不再匹配）
--   2. 转换后的值不会与其他行重复，无论该行已是 E.164 或待转换的遗留格式
--   3. 两条语句共同导致的汇聚也被保护（如 8613800138000 和 013800138000 都→ +8613800138000）
-- 若碰撞发生，两行均保持原样，供管理员手工介入。
--
-- DBA 查询：列出所有因碰撞而被保留未转的遗留号码（目标值已存在或被其他行独占）：
-- select basicid, phone,
--        case
--          when phone ~ '^86[0-9]{11}$' then '+' || phone
--          when phone ~ '^01[3-9][0-9]{9}$' then '+86' || substring(phone from 2)
--        end as target,
--        'taken_or_converged' as reason
--   from sys_user u1
--  where phone is not null
--    and (phone ~ '^86[0-9]{11}$' or phone ~ '^01[3-9][0-9]{9}$')
--    and exists (
--      select 1 from sys_user u2
--      where u2.basicid != u1.basicid
--        and (
--          -- Target value already exists in another row
--          u2.phone = case
--                       when u1.phone ~ '^86[0-9]{11}$' then '+' || u1.phone
--                       when u1.phone ~ '^01[3-9][0-9]{9}$' then '+86' || substring(u1.phone from 2)
--                     end
--          -- OR legacy row in opposite format would converge to same target
--          or (u1.phone ~ '^86[0-9]{11}$' and u2.phone ~ '^01[3-9][0-9]{9}$' and u2.phone = '0' || substring(u1.phone from 3))
--          or (u1.phone ~ '^01[3-9][0-9]{9}$' and u2.phone ~ '^86[0-9]{11}$' and u2.phone = '86' || substring(u1.phone from 2))
--        )
--    );

update sys_user
set phone = '+' || phone
where phone is not null
  and phone ~ '^86[0-9]{11}$'
  and not exists (
    select 1 from sys_user u2
    where u2.basicid != sys_user.basicid
      and (
        u2.phone = '+' || sys_user.phone
        or (u2.phone ~ '^01[3-9][0-9]{9}$' and '+86' || substring(u2.phone from 2) = '+' || sys_user.phone)
      )
  );

update sys_user
set phone = '+86' || substring(sys_user.phone from 2)
where phone is not null
  and phone ~ '^01[3-9][0-9]{9}$'
  and not exists (
    select 1 from sys_user u2
    where u2.basicid != sys_user.basicid
      and (
        u2.phone = '+86' || substring(sys_user.phone from 2)
        or (u2.phone ~ '^86[0-9]{11}$' and '+' || u2.phone = '+86' || substring(sys_user.phone from 2))
      )
  );
