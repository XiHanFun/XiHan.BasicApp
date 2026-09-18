-- 5.2.0
-- 存量手机号码统一为 E.164（+国码+号码），并把手机号唯一索引补齐到既有库上。
--
-- 手机号码成为登录身份标识后，存储写法必须与登录时提交的写法一致：
-- 库里存 0912345678 而登录提交 +886912345678，比对不上且不会有任何报错。
--
-- 本脚本只处理能无歧义转换的三类（均为中国大陆手机号的等价写法）：
--   1. 纯数字且以 86 开头、长度 13（861[3-9]xxxxxxxxx）→ 加 +
--   2. 以 0 开头的中国大陆十一位号码（0 + 1[3-9]xxxxxxxxx）→ 去 0 加 +86
--   3. 裸号码（1[3-9]xxxxxxxxx，十一位，不带任何国码/前缀）→ 加 +86
-- 其余写法（含其他国家的本地号码）保持原样：默认国码因部署而异，猜错会把号码改成别人的。
-- 未转换的号码在管理后台保存用户时会被前端国码选择器要求补全。
--
-- 幂等且碰撞安全：条件里带有 not exists 检查，确保：
--   1. 未以 + 开头的值才转换（跑过一次后不再匹配）
--   2. 转换后的值不会与其他行重复，无论该行已是 E.164 还是待转换的另外两种遗留格式
--   3. 三条语句两两之间的汇聚也被保护（如 8613800138000 / 013800138000 / 13800138000 都 → +8613800138000）
-- 若碰撞发生，涉及的行均保持原样，供管理员手工介入。
--
-- DBA 查询：列出所有因碰撞而被保留未转的遗留号码（目标值已存在或被其他行独占）：
-- select basicid, phone,
--        case
--          when phone ~ '^861[3-9][0-9]{9}$' then '+' || phone
--          when phone ~ '^01[3-9][0-9]{9}$' then '+86' || substring(phone from 2)
--          when phone ~ '^1[3-9][0-9]{9}$' then '+86' || phone
--        end as target,
--        'taken_or_converged' as reason
--   from sys_user u1
--  where phone is not null
--    and (phone ~ '^861[3-9][0-9]{9}$' or phone ~ '^01[3-9][0-9]{9}$' or phone ~ '^1[3-9][0-9]{9}$')
--    and exists (
--      select 1 from sys_user u2
--      where u2.basicid != u1.basicid
--        and (
--          -- 目标值已存在于另一行
--          u2.phone = case
--                       when u1.phone ~ '^861[3-9][0-9]{9}$' then '+' || u1.phone
--                       when u1.phone ~ '^01[3-9][0-9]{9}$' then '+86' || substring(u1.phone from 2)
--                       when u1.phone ~ '^1[3-9][0-9]{9}$' then '+86' || u1.phone
--                     end
--          -- 或另一行是会汇聚到同一目标的遗留写法
--          or (u1.phone ~ '^861[3-9][0-9]{9}$' and u2.phone ~ '^01[3-9][0-9]{9}$' and u2.phone = '0' || substring(u1.phone from 3))
--          or (u1.phone ~ '^861[3-9][0-9]{9}$' and u2.phone ~ '^1[3-9][0-9]{9}$' and u2.phone = substring(u1.phone from 3))
--          or (u1.phone ~ '^01[3-9][0-9]{9}$' and u2.phone ~ '^861[3-9][0-9]{9}$' and u2.phone = '86' || substring(u1.phone from 2))
--          or (u1.phone ~ '^01[3-9][0-9]{9}$' and u2.phone ~ '^1[3-9][0-9]{9}$' and u2.phone = substring(u1.phone from 2))
--          or (u1.phone ~ '^1[3-9][0-9]{9}$' and u2.phone ~ '^861[3-9][0-9]{9}$' and u2.phone = '86' || u1.phone)
--          or (u1.phone ~ '^1[3-9][0-9]{9}$' and u2.phone ~ '^01[3-9][0-9]{9}$' and u2.phone = '0' || u1.phone)
--        )
--    );
--
-- 唯一索引重复排查：库里仍有重复号码（含软删维度）时，下方 do $$ 块只会 raise notice、不建唯一索引：
-- select phone, isdeleted, count(*) from sys_user where phone is not null group by phone, isdeleted having count(*) > 1;

update sys_user
set phone = '+' || phone
where phone is not null
  and phone ~ '^861[3-9][0-9]{9}$'
  and not exists (
    select 1 from sys_user u2
    where u2.basicid != sys_user.basicid
      and (
        u2.phone = '+' || sys_user.phone
        or (u2.phone ~ '^01[3-9][0-9]{9}$' and '+86' || substring(u2.phone from 2) = '+' || sys_user.phone)
        or (u2.phone ~ '^1[3-9][0-9]{9}$' and '+86' || u2.phone = '+' || sys_user.phone)
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
        or (u2.phone ~ '^861[3-9][0-9]{9}$' and '+' || u2.phone = '+86' || substring(sys_user.phone from 2))
        or (u2.phone ~ '^1[3-9][0-9]{9}$' and '+86' || u2.phone = '+86' || substring(sys_user.phone from 2))
      )
  );

update sys_user
set phone = '+86' || phone
where phone is not null
  and phone ~ '^1[3-9][0-9]{9}$'
  and not exists (
    select 1 from sys_user u2
    where u2.basicid != sys_user.basicid
      and (
        u2.phone = '+86' || sys_user.phone
        or (u2.phone ~ '^861[3-9][0-9]{9}$' and '+' || u2.phone = '+86' || sys_user.phone)
        or (u2.phone ~ '^01[3-9][0-9]{9}$' and '+86' || substring(u2.phone from 2) = '+86' || sys_user.phone)
      )
  );

-- 空字符串手机号归一为 null：空串不受唯一索引约束（PostgreSQL 唯一索引不管多少行 NULL 都不算重复），
-- 但两行同为空字符串会被当成"相同值"撞唯一索引，必须先清成 null。
update sys_user set phone = null where phone = '';

-- 把 IX_sys_user_Ph（旧的非唯一索引，CodeFirst 建表时的产物名）升级为唯一索引。
-- XiHan.Framework 的 DbInitializer 对已存在的表跳过 CodeFirst，所以在本脚本之前，
-- 这台库上线过再升级的话，手机号唯一约束从未真正生效过——本节把它补上。
-- 新库：CodeFirst 已直接建出 ux_sys_user_ph，这里的 create index if not exists 是安全空转。
do $$
declare
  has_duplicates boolean;
  unique_index_exists boolean;
begin
  select exists (
    select 1
    from sys_user
    where phone is not null
    group by phone, isdeleted
    having count(*) > 1
  ) into has_duplicates;

  if not has_duplicates then
    create unique index if not exists ux_sys_user_ph on sys_user (phone, isdeleted);
  else
    -- 绝不因此中断升级：仍有重复留给管理员用脚本头部的 DBA 查询定位、人工处理后重跑本脚本
    raise notice 'sys_user.phone 仍有重复（按 phone+isdeleted 维度），跳过唯一索引创建。请用脚本头部的 DBA 查询定位重复行并人工处理后重跑本脚本。';
  end if;

  select exists (
    select 1 from pg_indexes where tablename = 'sys_user' and indexname = 'ux_sys_user_ph'
  ) into unique_index_exists;

  -- 唯一索引已就位才收尾旧的非唯一索引；未就位（上面因重复跳过）时保留旧索引，不让手机号查询失去索引
  if unique_index_exists then
    drop index if exists ix_sys_user_ph;
  end if;
end $$;
