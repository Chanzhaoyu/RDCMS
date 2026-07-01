# EF Core Migrations 使用指南

本文档记录 RDCMS 后端项目中常用的 Entity Framework Core 数据库迁移命令。

## 目录

- [项目约定](#项目约定)
- [首次准备](#首次准备)
- [最常用流程](#最常用流程)
- [迁移命令](#迁移命令)
- [数据库命令](#数据库命令)
- [SQL 脚本](#sql-脚本)
- [常见问题](#常见问题)

## 项目约定

请先进入后端目录：

```bash
cd backend
```

本项目的迁移相关配置如下：

| 配置项 | 值 |
| --- | --- |
| DbContext | `AppDbContext` |
| DbContext 所在项目 | `RDCMS.Infrastructure` |
| 启动项目 | `RDCMS.Api` |
| 迁移文件目录 | `RDCMS.Infrastructure/Data/Migrations` |

所以命令中通常都需要带上这些参数：

```bash
--project RDCMS.Infrastructure \
--startup-project RDCMS.Api \
--context AppDbContext
```

创建迁移时还需要额外指定迁移输出目录：

```bash
--output-dir Data/Migrations
```

## 首次准备

### 1. 安装 EF Core CLI

如果本机没有安装 `dotnet-ef`：

```bash
dotnet tool install --global dotnet-ef --version "8.*"
```

如果已经安装过，可以更新：

```bash
dotnet tool update --global dotnet-ef --version "8.*"
```

验证是否安装成功：

```bash
dotnet ef --version
```

### 2. 安装 EF Core Design 包

EF Core Tools 需要启动项目引用 `Microsoft.EntityFrameworkCore.Design`。

本项目启动项目是 `RDCMS.Api`，如果迁移时报下面这个错误：

```text
Your startup project 'RDCMS.Api' doesn't reference Microsoft.EntityFrameworkCore.Design.
```

执行：

```bash
dotnet add RDCMS.Api package Microsoft.EntityFrameworkCore.Design --version 8.0.13
```

## 最常用流程

修改实体后，一般只需要两步。

### 1. 创建迁移

把 `迁移名称` 换成有意义的名称，例如 `AddUserEmailIndex`、`CreateUserTable`。

```bash
dotnet ef migrations add 迁移名称 \
  --project RDCMS.Infrastructure \
  --startup-project RDCMS.Api \
  --context AppDbContext \
  --output-dir Data/Migrations
```

### 2. 更新数据库

```bash
dotnet ef database update \
  --project RDCMS.Infrastructure \
  --startup-project RDCMS.Api \
  --context AppDbContext
```

## 迁移命令

### 创建初始迁移

```bash
dotnet ef migrations add InitialCreate \
  --project RDCMS.Infrastructure \
  --startup-project RDCMS.Api \
  --context AppDbContext \
  --output-dir Data/Migrations
```

### 创建新的迁移

```bash
dotnet ef migrations add AddUserEmailIndex \
  --project RDCMS.Infrastructure \
  --startup-project RDCMS.Api \
  --context AppDbContext \
  --output-dir Data/Migrations
```

### 查看迁移列表

```bash
dotnet ef migrations list \
  --project RDCMS.Infrastructure \
  --startup-project RDCMS.Api \
  --context AppDbContext
```

### 删除最后一个迁移

如果刚创建的迁移还没有更新到数据库，可以删除最后一个迁移：

```bash
dotnet ef migrations remove \
  --project RDCMS.Infrastructure \
  --startup-project RDCMS.Api \
  --context AppDbContext
```

> 如果这个迁移已经更新到数据库，需要先回滚数据库，再删除迁移文件。

## 数据库命令

### 更新到最新迁移

把所有未应用的迁移更新到数据库：

```bash
dotnet ef database update \
  --project RDCMS.Infrastructure \
  --startup-project RDCMS.Api \
  --context AppDbContext
```

### 更新到指定迁移

```bash
dotnet ef database update 迁移名称 \
  --project RDCMS.Infrastructure \
  --startup-project RDCMS.Api \
  --context AppDbContext
```

### 回滚到指定迁移

EF Core 的回滚也是使用 `database update`。

例如回滚到 `InitialCreate`：

```bash
dotnet ef database update InitialCreate \
  --project RDCMS.Infrastructure \
  --startup-project RDCMS.Api \
  --context AppDbContext
```

### 回滚到空数据库状态

```bash
dotnet ef database update 0 \
  --project RDCMS.Infrastructure \
  --startup-project RDCMS.Api \
  --context AppDbContext
```

### 删除数据库

开发环境需要完全重建数据库时可以使用：

```bash
dotnet ef database drop \
  --project RDCMS.Infrastructure \
  --startup-project RDCMS.Api \
  --context AppDbContext
```

> 注意：这个命令会删除数据库。执行前一定要确认当前连接的是开发环境数据库。

## SQL 脚本

### 生成从空数据库到最新迁移的 SQL

```bash
dotnet ef migrations script \
  --project RDCMS.Infrastructure \
  --startup-project RDCMS.Api \
  --context AppDbContext \
  --output migration.sql
```

### 生成指定迁移区间的 SQL

```bash
dotnet ef migrations script 起始迁移名称 目标迁移名称 \
  --project RDCMS.Infrastructure \
  --startup-project RDCMS.Api \
  --context AppDbContext \
  --output migration.sql
```

例如：

```bash
dotnet ef migrations script InitialCreate AddUserEmailIndex \
  --project RDCMS.Infrastructure \
  --startup-project RDCMS.Api \
  --context AppDbContext \
  --output migration.sql
```

## 常见问题

### 启动项目缺少 Microsoft.EntityFrameworkCore.Design

错误信息：

```text
Your startup project 'RDCMS.Api' doesn't reference Microsoft.EntityFrameworkCore.Design.
```

解决方式：

```bash
dotnet add RDCMS.Api package Microsoft.EntityFrameworkCore.Design --version 8.0.13
```

### 修改实体后没有生成任何变更

可以检查：

1. 实体是否已经加入 `AppDbContext` 的 `DbSet`。
2. 实体配置是否已经在 `OnModelCreating` 中生效。
3. 是否在正确目录执行命令，也就是 `backend` 目录。
4. `--project` 和 `--startup-project` 是否写反。

### 想重建开发数据库

开发环境可以按下面流程重建数据库：

```bash
dotnet ef database drop \
  --project RDCMS.Infrastructure \
  --startup-project RDCMS.Api \
  --context AppDbContext

dotnet ef database update \
  --project RDCMS.Infrastructure \
  --startup-project RDCMS.Api \
  --context AppDbContext
```

> 注意：只建议在本地开发环境使用，不能对生产数据库执行。