![logo](./assets/logo.png)

[English](README.md)

# XiHan.BasicApp

曦寒应用存储库。通用、全面的管理系统，基于曦寒框架和曦寒界面构建。

[![Ask DeepWiki](https://deepwiki.com/badge.svg)](https://deepwiki.com/XiHanFun/XiHan.BasicApp)

## 📋 项目概述

**XiHan.BasicApp** 是一个通用、全面的管理系统，基于 XiHan.Framework 和 XiHan.UI 构建。采用现代化的模块化架构设计，提供完整的企业级应用解决方案。

### 🎯 项目目标

- 构建通用的企业级管理系统
- 提供模块化、可扩展的架构
- 实现前后端分离的现代化应用
- 建立完整的权限管理体系
- 支持代码自动生成功能

## 🏗️ 技术架构

### 后端技术栈

- **.NET 10.0** - 最新的.NET 平台
- **DDD 架构** - 领域驱动设计
- **模块化设计** - 松耦合的模块化架构
- **Serilog** - 结构化日志记录
- **XiHan.Framework** - 自研框架基础

### 前端技术栈

- **Vue.js** - 渐进式前端框架
- **XiHan.UI** - 专用组件库
- **现代化构建工具** - 支持 npm/yarn/pnpm

## 📁 项目结构分析

### 后端模块架构

```
backend/src/
├── framework/           # 框架层
│   ├── Core/           # 核心框架
│   └── AspNetCore/     # Web框架扩展
├── modules/            # 功能模块层
│   ├── Rbac/          # 权限管理模块
│   └── CodeGeneration/ # 代码生成模块
├── business/          # 业务模块层
│   └── Blog/          # 博客业务模块
└── main/              # 主应用层
    └── WebHost/       # Web主机应用
```

### 前端结构规划

```
frontend/
├── src/
│   ├── components/    # 通用组件
│   ├── views/         # 页面视图
│   ├── modules/       # 功能模块
│   ├── utils/         # 工具函数
│   └── api/           # API接口
├── public/            # 静态资源
└── docs/              # 文档
```
