#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
OpenAPI 签名网关端到端自测脚本
================================

验证「签发凭证 → HMAC 验签 → 防重放 → 凭证解析 → 放行」整条链路。

对齐后端：
  - 中间件 XiHanOpenApiSecurityMiddleware（规范串 method\\npath\\nquery\\ncontentSign\\ntimestamp\\nnonce）
  - 存储 SaasOpenApiSecurityClientStore（DB 凭证 SysUserApiCredential，密钥可逆加密）
  - 自测端点 OpenApiAppService（动态 API 按约定自动注册）：/api/openapi/Ping (GET) 与 /api/openapi/Echo (POST)
    响应走应用统一信封 {code,message,data}，脚本断言前会剥离

前置：
  1) 后端已运行（默认 http://127.0.0.1:9708），且 appsettings.Development.json 里
     XiHan:Web:Api:OpenApiSecurity.IsEnabled=true、ProtectedPathPrefixes 覆盖 /api/openapi。
  2) pip install requests

用法：
  # 全自动：登录超管 → 自助创建一枚 DB 凭证 → 跑全部用例 → 清理凭证
  # （Windows 上 python 可能命中商店占位程序而静默无输出，请改用 py）
  py openapi_signature_test.py

  # 指定后端地址 / 账号
  python openapi_signature_test.py --base-url http://127.0.0.1:9708 --username superadmin --password "SuperAdmin@123"

  # 跳过登录，直接用已有 AppKey/AppSecret（例如开发者中心页面创建的，或配置客户端）
  python openapi_signature_test.py --access-key ak_xxx --secret-key sk_xxx
"""

import argparse
import hashlib
import hmac
import json
import sys
import time
from secrets import token_hex
from urllib.parse import quote, urlsplit, parse_qsl

try:
    import requests
except ImportError:
    print("缺少依赖：请先执行  pip install requests")
    sys.exit(1)


# 开放接口自测端点（动态 API，由 OpenApiAppService 按约定自动注册；RouteTemplate=api/openapi）
PING_PATH = "/api/openapi/Ping"
ECHO_PATH = "/api/openapi/Echo"


# ---------------------------------------------------------------------------
# 签名（与后端中间件逐字节对齐）
# ---------------------------------------------------------------------------

def _sha_hex(algorithm: str, text: str) -> str:
    """内容签名：SHA256/SHA512 十六进制小写"""
    data = text.encode("utf-8")
    digest = hashlib.sha512(data) if algorithm.upper() == "SHA512" else hashlib.sha256(data)
    return digest.hexdigest()


def _hmac_hex(algorithm: str, secret: str, message: str) -> str:
    """请求签名：HMAC-SHA256/512 十六进制小写（服务端按 OrdinalIgnoreCase 比对，大小写无关）"""
    mod = hashlib.sha512 if algorithm.upper() == "HMACSHA512" else hashlib.sha256
    return hmac.new(secret.encode("utf-8"), message.encode("utf-8"), mod).hexdigest()


def _canonical_query(query_string: str) -> str:
    """规范化查询串：按 键→值 排序，键值分别 RFC3986 编码，用 & 连接（对齐 Uri.EscapeDataString）"""
    if not query_string:
        return ""
    pairs = parse_qsl(query_string, keep_blank_values=True)
    pairs.sort(key=lambda kv: (kv[0], kv[1]))
    return "&".join(f"{quote(k, safe='')}={quote(v, safe='')}" for k, v in pairs)


def build_signed_headers(method, path, query_string, body, access_key, secret_key,
                         sign_alg="HMACSHA256", content_sign_alg="SHA256",
                         timestamp=None, nonce=None):
    """构造一整套签名请求头。timestamp/nonce 可显式传入以构造过期/重放用例。"""
    method = method.upper()
    nonce = nonce or token_hex(16)
    timestamp = timestamp or str(int(time.time()))
    content_sign = _sha_hex(content_sign_alg, body or "")
    canonical_query = _canonical_query(query_string)
    canonical = "\n".join([method, path, canonical_query, content_sign, timestamp, nonce])
    signature = _hmac_hex(sign_alg, secret_key, canonical)
    headers = {
        "X-Access-Key": access_key,
        "X-Timestamp": timestamp,
        "X-Nonce": nonce,
        "X-Signature": signature,
        "X-Content-Sign": content_sign,
        "X-Sign-Algorithm": sign_alg,
        "X-Content-Sign-Algorithm": content_sign_alg,
        # 关键：显式声明不加密，否则中间件按全局默认 AES-CBC 尝试解密明文体而失败
        "X-Encrypt-Algorithm": "NONE",
    }
    return headers, canonical


# ---------------------------------------------------------------------------
# 后端交互
# ---------------------------------------------------------------------------

def _unwrap(payload):
    """剥离统一响应信封 {code,message,data,...}，取 data；非信封则原样返回。"""
    if isinstance(payload, dict) and "data" in payload:
        return payload["data"]
    return payload


def login(base_url, username, password):
    url = f"{base_url}/api/Auth/Login"
    resp = requests.post(url, json={"username": username, "password": password}, timeout=15)
    resp.raise_for_status()
    data = _unwrap(resp.json())
    if isinstance(data, dict) and data.get("requiresTwoFactor"):
        raise RuntimeError("该账号开启了两步验证，无法自动登录；请改用 --access-key/--secret-key 直连测试。")
    token = (data or {}).get("token") if isinstance(data, dict) else None
    access_token = (token or data or {}).get("accessToken") if isinstance(token or data, dict) else None
    if not access_token:
        raise RuntimeError(f"登录未取得访问令牌，原始响应：{resp.text[:500]}")
    return access_token


def create_credential(base_url, access_token, name):
    url = f"{base_url}/api/Profile/ApiCredential"
    resp = requests.post(url, json={"credentialName": name},
                         headers={"Authorization": f"Bearer {access_token}"}, timeout=15)
    resp.raise_for_status()
    data = _unwrap(resp.json())
    app_key = data.get("appKey")
    app_secret = data.get("appSecret")
    basic_id = data.get("basicId")
    if not app_key or not app_secret:
        raise RuntimeError(f"创建凭证未取得 AppKey/AppSecret，原始响应：{resp.text[:500]}")
    return app_key, app_secret, basic_id


def delete_credential(base_url, access_token, basic_id):
    try:
        requests.delete(f"{base_url}/api/Profile/ApiCredential/{basic_id}",
                        headers={"Authorization": f"Bearer {access_token}"}, timeout=15)
    except Exception:
        pass


# ---------------------------------------------------------------------------
# 用例
# ---------------------------------------------------------------------------

class Runner:
    def __init__(self, base_url, access_key, secret_key):
        self.base_url = base_url
        self.access_key = access_key
        self.secret_key = secret_key
        self.passed = 0
        self.failed = 0

    def _check(self, name, ok, detail=""):
        flag = "✓ PASS" if ok else "✗ FAIL"
        print(f"  [{flag}] {name}" + (f"  —— {detail}" if detail else ""))
        if ok:
            self.passed += 1
        else:
            self.failed += 1

    def ping_signed(self):
        path = PING_PATH
        headers, canonical = build_signed_headers("GET", path, "", "", self.access_key, self.secret_key)
        resp = requests.get(f"{self.base_url}{path}", headers=headers, timeout=15)
        data = _unwrap(_safe_json(resp))
        ok = resp.status_code == 200 and isinstance(data, dict) and data.get("ok") is True \
            and data.get("accessKey") == self.access_key
        print("    规范串 canonical =", repr(canonical))
        self._check(f"签名 GET {PING_PATH} → 200 且回显本 AccessKey", ok,
                    f"HTTP {resp.status_code}, accessKey={data.get('accessKey') if isinstance(data, dict) else data}")

    def echo_signed(self):
        path = ECHO_PATH
        payload = json.dumps({"message": "xihan", "number": 42}, separators=(",", ":"), ensure_ascii=False)
        headers, _ = build_signed_headers("POST", path, "", payload, self.access_key, self.secret_key)
        headers["Content-Type"] = "application/json"
        resp = requests.post(f"{self.base_url}{path}", data=payload.encode("utf-8"), headers=headers, timeout=15)
        data = _unwrap(_safe_json(resp))
        echo = data.get("echo") if isinstance(data, dict) else None
        ok = resp.status_code == 200 and isinstance(data, dict) and data.get("ok") is True \
            and isinstance(echo, dict) and echo.get("message") == "xihan" and echo.get("number") == 42
        self._check(f"签名 POST {ECHO_PATH} → 200 且请求体完整回显", ok,
                    f"HTTP {resp.status_code}, echo={echo}")

    def unsigned_rejected(self):
        resp = requests.get(f"{self.base_url}{PING_PATH}", timeout=15)
        self._check("未签名请求 → 401 拒绝", resp.status_code == 401, f"HTTP {resp.status_code}")

    def tampered_rejected(self):
        headers, _ = build_signed_headers("GET", PING_PATH, "", "", self.access_key, self.secret_key)
        headers["X-Signature"] = "deadbeef" + headers["X-Signature"][8:]
        resp = requests.get(f"{self.base_url}{PING_PATH}", headers=headers, timeout=15)
        self._check("篡改签名 → 401 拒绝", resp.status_code == 401, f"HTTP {resp.status_code}")

    def expired_rejected(self):
        old_ts = str(int(time.time()) - 400)  # 超出默认 300s 容差
        headers, _ = build_signed_headers("GET", PING_PATH, "", "", self.access_key, self.secret_key, timestamp=old_ts)
        resp = requests.get(f"{self.base_url}{PING_PATH}", headers=headers, timeout=15)
        self._check("过期时间戳（>300s）→ 401 拒绝", resp.status_code == 401, f"HTTP {resp.status_code}")

    def replay_rejected(self):
        path = PING_PATH
        nonce = token_hex(16)
        ts = str(int(time.time()))
        headers, _ = build_signed_headers("GET", path, "", "", self.access_key, self.secret_key, timestamp=ts, nonce=nonce)
        first = requests.get(f"{self.base_url}{path}", headers=headers, timeout=15)
        second = requests.get(f"{self.base_url}{path}", headers=headers, timeout=15)
        self._check("重放同一 nonce → 首次 200、重放 409", first.status_code == 200 and second.status_code == 409,
                    f"第一次 HTTP {first.status_code}，重放 HTTP {second.status_code}")

    def run_all(self):
        print("\n开始用例：")
        self.ping_signed()
        self.echo_signed()
        self.unsigned_rejected()
        self.tampered_rejected()
        self.expired_rejected()
        self.replay_rejected()
        print(f"\n结果：{self.passed} 通过 / {self.failed} 失败")
        return self.failed == 0


def _safe_json(resp):
    try:
        return resp.json()
    except Exception:
        return resp.text


# ---------------------------------------------------------------------------
# 主流程
# ---------------------------------------------------------------------------

def main():
    parser = argparse.ArgumentParser(description="OpenAPI 签名网关端到端自测")
    parser.add_argument("--base-url", default="http://127.0.0.1:9708", help="后端地址（默认 dev Kestrel 端口）")
    parser.add_argument("--username", default="superadmin")
    parser.add_argument("--password", default="SuperAdmin@123")
    parser.add_argument("--access-key", default=None, help="直连模式：跳过登录，使用现有 AppKey")
    parser.add_argument("--secret-key", default=None, help="直连模式：使用现有 AppSecret")
    args = parser.parse_args()

    base_url = args.base_url.rstrip("/")
    print(f"后端地址：{base_url}")

    access_token = None
    created_id = None

    if args.access_key and args.secret_key:
        print("模式：直连（使用传入的 AppKey/AppSecret）")
        access_key, secret_key = args.access_key, args.secret_key
    else:
        print(f"模式：自动（登录 {args.username} → 创建 DB 凭证）")
        try:
            access_token = login(base_url, args.username, args.password)
            print("  登录成功，已取得访问令牌")
            access_key, secret_key, created_id = create_credential(base_url, access_token, "openapi-selftest")
            print(f"  已创建测试凭证 AppKey={access_key}（AppSecret 明文仅本次可见）")
        except Exception as ex:
            print(f"\n[准备阶段失败] {ex}")
            print("提示：确认后端已启动、账号密码正确；或改用 --access-key/--secret-key 直连模式。")
            sys.exit(2)

    try:
        ok = Runner(base_url, access_key, secret_key).run_all()
    finally:
        if access_token and created_id:
            delete_credential(base_url, access_token, created_id)
            print("已清理测试凭证。")

    sys.exit(0 if ok else 1)


if __name__ == "__main__":
    main()
