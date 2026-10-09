# Assignment 12.3

One numbered C# source file per assignment question, with separate runnable projects.

| Question | Source | Project |
| --- | --- | --- |
| 1: Remove adjacent duplicates (LeetCode #1047) | `12.3.1.cs` | `12.3.1/12.3.1.csproj` (console) |
| 2: SQLite CRUD with .NET MAUI | `12.3.2.cs` | `12.3.2/12.3.2.csproj` (Windows MAUI app) |

The numbered files are at the repository root and linked into their projects. Question two contains the student model, database operations, MAUI page, and app startup in one file. The Windows platform startup file is supporting framework code.

## Open in Visual Studio

1. Open **12.3.sln**.
2. Right-click **12.3.1** or **12.3.2**, then select **Set as Startup Project**.
3. For question two, select **Windows Machine**.
4. Press **Ctrl+F5**.

Requires .NET 10. Question two also requires Visual Studio's **.NET Multi-platform App UI development** workload and Windows 10 version 1809 or later. NuGet restore requires internet access. This MAUI project targets Windows.

## Question one

Use a character array as a stack. If the next letter matches the top, pop that letter and skip the current one. Otherwise push the current letter. Newly adjacent duplicates are removed naturally as the input is scanned. Return the remaining characters.

Time: **O(n)**. Extra space: **O(n)**.

```text
abbaca -> ca
azxxzy -> ay
```

```powershell
dotnet run --project 12.3.1 -- --examples
dotnet run --project 12.3.1 -- abbaca
```

Without arguments, the console prompts for lowercase English letters. An empty string returns an empty string.

## Question two

The student manager performs all four operations against a real local SQLite database:

- **Create:** enter a name and course, then click **Add**.
- **Read:** saved records load automatically; **Refresh** reads them again.
- **Update:** select a student, edit the fields, then click **Update**.
- **Delete:** select a student, click **Delete**, and confirm.

**Clear** resets the editor without deleting saved records. Names and courses are required. SQL parameters handle input containing apostrophes safely. The database is `assignment12_3_students.db3` under MAUI's `FileSystem.AppDataDirectory`; records persist after closing and reopening the app. Update and delete target the selected primary key.

This app uses `Microsoft.Data.Sqlite`, an alternative documented in [Microsoft's .NET MAUI local database guide](https://learn.microsoft.com/en-us/dotnet/maui/data-cloud/database-sqlite?view=net-maui-10.0). Connections are disposed after each operation. SQLite executes its database work synchronously even when using the provider's async methods; this is a small local classroom app.

## Build and verify

From this folder:

```powershell
dotnet build 12.3.sln -c Release
dotnet run --project Tests/Checks.csproj -c Release
```

The checks exercise the actual numbered source files: both assignment examples, duplicate cascades and edge cases, plus real SQLite create/read/update/delete, persistence, validation, and literal SQL-like input. The check harness excludes the MAUI UI through a compile symbol and runs without the MAUI workload. Build outputs and local database files are excluded from Git.
