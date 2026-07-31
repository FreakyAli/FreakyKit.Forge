---
name: Bug report
about: Report a bug in Forge
title: ''
labels: bug
assignees: ''

---

**Forge version**
<!-- e.g. 1.5.0 -->

**.NET SDK version**
<!-- Run `dotnet --version` and paste the output -->

**Target framework**
<!-- e.g. net8.0, net9.0 -->

**IDE / build tool**
<!-- e.g. Visual Studio 2022, Rider 2025.1, `dotnet build` -->

**Describe the bug**
A clear description of what the bug is.

**Minimal reproduction**
<!-- Paste the smallest source types + forge class that reproduces the issue -->

```csharp
// Source and destination types
public class Source { /* ... */ }
public class Dest { /* ... */ }

// Forge class
[Forge]
public static partial class MyForges
{
    public static partial Dest ToDto(Source source);
}
```

**Expected behavior**
What you expected to happen.

**Actual behavior**
What actually happened. Include any diagnostic output or generated code if applicable.

**Additional context**
Any other context about the problem.
