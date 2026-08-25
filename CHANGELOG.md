# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased][Unreleased]

* **Breaking change**: Drop support for .NET 8, EF Core 8 and 9.

  `WorkaroundSqlClientIssue26` now requires .NET 10 and EF Core 10. If you are still on an older EF Core version, you can keep using version 1.0.0.

* Add support for [Azure SQL](https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.sqlserverdbcontextoptionsextensions.useazuresql) and [Azure Synapse](https://learn.microsoft.com/en-us/dotnet/api/microsoft.entityframeworkcore.sqlserverdbcontextoptionsextensions.useazuresynapse)

```c#
var options = new DbContextOptionsBuilder<MyDbContext>()
  .UseAzureSql(connectionString, s => s.WorkAroundSqlClientIssue26())
  .Options;
```

```c#
var options = new DbContextOptionsBuilder<MyDbContext>()
  .UseAzureSynapse(connectionString, s => s.WorkAroundSqlClientIssue26())
  .Options;
```

## [1.0.0][1.0.0] - 2026-04-27

Initial release

[Unreleased]: https://github.com/0xced/WorkaroundSqlClientIssue26/compare/1.0.0...HEAD
[1.0.0]: https://github.com/0xced/WorkaroundSqlClientIssue26/compare/cb129372dc92110199ef116aeced11ef5babaebf...1.0.0
