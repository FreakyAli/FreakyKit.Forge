# Contributing to Forge

Thank you for your interest in contributing to Forge! This document provides guidelines and instructions for contributing code, documentation, tests, and bug reports.

## Table of Contents

- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
- [Project Structure](#project-structure)
- [Development Workflow](#development-workflow)
- [Running Tests](#running-tests)
- [Common Workflows](#common-workflows)
- [Build and Debug Tips](#build-and-debug-tips)
- [Troubleshooting](#troubleshooting)
- [Code Style and Patterns](#code-style-and-patterns)
- [Key Concepts](#key-concepts)
- [Adding Features](#adding-features)
- [Submitting Changes](#submitting-changes)
- [Documentation](#documentation)
- [First-Time Contributor Guide](#first-time-contributor-guide)
- [Questions and Support](#questions-and-support)

## Code of Conduct

We are committed to providing a welcoming and inclusive environment. Please be respectful, constructive, and professional in all interactions.

## Getting Started

### Prerequisites

- .NET 9.0 SDK (pinned via `global.json`; 9.0.100 or later feature band)
- C# knowledge (this is a Roslyn toolchain project)
- Familiarity with source generators and analyzers is helpful but not required

### Local Setup

1. **Clone the repository:**
   ```bash
   git clone https://github.com/FreakyAli/FreakyKit.Forge.git
   cd FreakyKit.Forge
   ```

2. **Build the solution:**
   ```bash
   dotnet build
   ```

3. **Run all tests:**
   ```bash
   dotnet test
   ```

4. **Verify everything works:**
   ```bash
   dotnet build --no-incremental
   dotnet test --no-build
   ```

All tests should pass. See [Running Tests](#running-tests) for more details.

## Project Structure

```
FreakyKit.Forge/
├── src/
│   ├── FreakyKit.Forge/                    # Core attributes and enums
│   │   ├── ForgeAttribute.cs
│   │   ├── ForgeMethodAttribute.cs
│   │   ├── ForgeMode.cs
│   │   └── ... (other attributes)
│   ├── FreakyKit.Forge.Generator/          # Source generator (netstandard2.0)
│   │   ├── ForgeGenerator.cs               # Main generator entry point
│   │   ├── Models/ForgeMethodModel.cs      # Code generation data model
│   │   └── ...
│   └── FreakyKit.Forge.Analyzers/          # Roslyn analyzer (netstandard2.0)
│       ├── ForgeAnalyzer.cs                # Main analyzer entry point
│       └── ...
├── tests/
│   ├── FreakyKit.Forge.Generator.Tests/    # Generator unit tests (net8.0)
│   ├── FreakyKit.Forge.Analyzers.Tests/    # Analyzer unit tests (net8.0)
│   └── FreakyKit.Forge.Integration.Tests/  # End-to-end integration tests (net8.0)
├── docs/                                    # User documentation
└── samples/                                 # Example projects
```

### Key Files

| File | Purpose |
|------|---------|
| `src/FreakyKit.Forge.Analyzers/ForgeAnalyzer.cs` | Main analyzer logic |
| `src/FreakyKit.Forge.Generator/ForgeGenerator.cs` | Main generator logic |
| `src/FreakyKit.Forge.Generator/Models/ForgeMethodModel.cs` | Data model for code generation |

## Development Workflow

### Before Starting

1. Check existing [issues](https://github.com/FreakyAli/FreakyKit.Forge/issues) and [pull requests](https://github.com/FreakyAli/FreakyKit.Forge/pulls) to avoid duplicate work
2. For feature requests or bug reports, open an issue first to discuss
3. Create a new branch from `master`:
   ```bash
   git checkout -b fix/issue-description
   # or
   git checkout -b feat/feature-description
   ```

### During Development

1. **Write tests first** — Follow test-driven development (TDD):
   - Add failing tests for your change
   - Implement the feature to make tests pass
   - Refactor as needed

2. **Keep changes focused** — One logical change per branch

3. **Rebuild after changes** — If modifying the generator or analyzer:
   ```bash
   dotnet build
   ```

4. **Run tests frequently** — Both unit and integration tests:
   ```bash
   dotnet test
   ```

### Integration Tests Require Full Rebuild

After editing source in the generator or analyzer, always rebuild before running integration tests:

```bash
dotnet build                          # Rebuild binaries
dotnet test --project tests/FreakyKit.Forge.Integration.Tests  # Run integration tests
```

Do NOT use `--no-build` against stale binaries — it masks real failures.

## Running Tests

### Run All Tests

```bash
dotnet test
```

### Run Tests for a Specific Project

```bash
# Generator tests only
dotnet test --project tests/FreakyKit.Forge.Generator.Tests

# Analyzer tests only
dotnet test --project tests/FreakyKit.Forge.Analyzers.Tests

# Integration tests only
dotnet test --project tests/FreakyKit.Forge.Integration.Tests
```

### Run a Specific Test

```bash
dotnet test --filter "FullyQualifiedName~TestName"
```

### Test Projects

- **Generator Tests** — Snapshot tests for generated code, model transformations, edge cases
- **Analyzer Tests** — Diagnostic rules, error messages, code analysis validation
- **Integration Tests** — End-to-end scenarios combining generator + analyzer

## Common Workflows

### Fixing a Bug

1. Open or reference the issue that describes the bug
2. Create a new branch: `git checkout -b fix/bug-description`
3. Write a failing test that reproduces the bug
4. Fix the bug in the source code
5. Run tests to verify the fix: `dotnet test`
6. If integration tests are affected, rebuild and retest:
   ```bash
   dotnet build
   dotnet test --project tests/FreakyKit.Forge.Integration.Tests
   ```
7. Commit your changes with a clear message
8. Push and open a PR

### Adding a New Feature

1. Open an issue or discussion to propose the feature
2. Get feedback from maintainers before implementing
3. Create a new branch: `git checkout -b feat/feature-name`
4. Write tests for the desired behavior
5. Implement the feature to make tests pass
6. Add/update documentation in `docs/`
7. Run full test suite: `dotnet build && dotnet test`
8. Commit and open a PR

### Exploring the Codebase

- **Generator logic** — Start in `src/FreakyKit.Forge.Generator/ForgeGenerator.cs`
- **Analyzer logic** — Start in `src/FreakyKit.Forge.Analyzers/ForgeAnalyzer.cs`
- **Test examples** — Look at snapshot tests to see expected behavior
- **Generated output** — Check `.g.cs` files in test output to understand what gets generated

## Build and Debug Tips

### Incremental vs Full Builds

```bash
# Full rebuild (slower but ensures clean state)
dotnet build --no-incremental

# Incremental build (faster for iteration)
dotnet build
```

### Inspecting Generated Code

1. Build the project: `dotnet build`
2. Check `obj/` directory for generated `.g.cs` files
3. Look at test snapshot files in `tests/` for captured output
4. Run integration tests to see real-world generated code

### Debugging the Generator

1. Create a test that reproduces the issue
2. Set breakpoints in generator code
3. Run the test with a debugger:
   ```bash
   dotnet test --project tests/FreakyKit.Forge.Generator.Tests --filter "TestName"
   ```
4. Step through the code to understand the flow

### Debugging the Analyzer

1. Create a test case
2. Set breakpoints in analyzer code
3. Run analyzer tests with debugger:
   ```bash
   dotnet test --project tests/FreakyKit.Forge.Analyzers.Tests --filter "TestName"
   ```

## Troubleshooting

### Tests Fail After Changing Generator/Analyzer Code

**Problem:** Integration tests fail after modifying source code

**Solution:** Always rebuild before running integration tests:
```bash
dotnet build                    # Rebuild binaries
dotnet test --no-build          # Use fresh binaries
```

**Why:** Integration tests load the compiled generator/analyzer from disk. Stale binaries cause misleading failures.

### Test Timeouts or Hangs

**Problem:** Tests hang or timeout during execution

**Solution:**
1. Check if you have infinite loops in generator/analyzer code
2. Try running a single test to isolate the issue:
   ```bash
   dotnet test --filter "TestName"
   ```
3. Look at test setup — ensure cleanup code runs properly

### Snapshot Test Failures

**Problem:** Snapshot tests fail after code changes

**Solution:**
1. Review the diff to ensure the generated code looks correct
2. If intentional, update the snapshot:
   - Most snapshot libraries have an "accept" or "update" mode
   - Check the test project's snapshot testing configuration
3. Commit the updated snapshots with your change

### Build Errors in Generated Code

**Problem:** Compilation fails with errors in generated `.g.cs` files

**Solution:**
1. Check the source generator code for typos or logic errors
2. Write a test that captures the issue
3. Fix the generator logic
4. Rebuild and test again

## Code Style and Patterns

### General Guidelines

- Use meaningful variable and method names
- Keep methods focused and small
- Write comments only for non-obvious logic (the "why", not the "what")
- Follow existing code conventions in the file you're editing

### Code Generation Guidelines

When writing generator code, follow the existing patterns in the codebase:

- Study how the current generator creates code in `ForgeGenerator.cs` and related files
- Look at snapshot tests in `FreakyKit.Forge.Generator.Tests/` to understand expected output
- Check the generated `.g.cs` files in test projects to see real examples
- Refer to the Roslyn documentation when working with symbol and syntax APIs

When modifying analyzer code, check existing rules in `ForgeAnalyzer.cs` and analyzer tests for patterns.

## Key Concepts

### Testing

This project uses unit tests and snapshot tests extensively:

- **Unit tests** validate logic, transformations, and edge cases
- **Snapshot tests** capture generated output to catch regressions
- **Integration tests** verify end-to-end behavior with real compilations

Look at existing tests in `tests/` for patterns and examples.

### Generator & Analyzer

The project consists of two main components:

- **Generator** — Runs at compile time to generate mapping code
- **Analyzer** — Provides build-time diagnostics and validation

Both are standard Roslyn components. Refer to existing source files and tests when making changes.

## Adding Features

### When Adding a Feature

1. **Start with a test** — Write a failing test that demonstrates the desired behavior
2. **Implement the feature** — Add the minimum code to make the test pass
3. **Add appropriate tests** — Follow the existing test patterns in the project
4. **Update documentation** — Add docs to `docs/` explaining the feature
5. **Update `README.md`** if it affects user-facing APIs

### Feature Checklist

- [ ] Tests written and passing
- [ ] Documentation updated
- [ ] README updated (if user-facing)
- [ ] No breaking changes (or documented as breaking)
- [ ] All tests pass locally

### Backward Compatibility

Maintain backward compatibility unless there's a strong reason not to:

- Old code using previous versions should continue to work
- If breaking changes are necessary, document them clearly
- Consider deprecation warnings before removing features

## Submitting Changes

### Before Submitting

1. **Pull latest from master:**
   ```bash
   git checkout master
   git pull origin master
   ```

2. **Rebase your branch:**
   ```bash
   git checkout your-branch
   git rebase master
   ```

3. **Run full test suite:**
   ```bash
   dotnet build
   dotnet test
   ```

4. **Verify no breaking changes** by running the example projects

### Creating a Pull Request

1. **Push your branch to GitHub:**
   ```bash
   git push origin your-branch
   ```

2. **Open a pull request** with:
   - Clear title describing the change
   - Description of what changed and why
   - Reference to related issues (`Fixes #123`)
   - Any breaking changes clearly marked
   - Screenshots/diffs for visual changes

### PR Checklist

- [ ] All tests pass locally
- [ ] Tests added for new features
- [ ] Documentation updated
- [ ] No unrelated changes included
- [ ] Branch is rebased on latest `master`
- [ ] Commit messages are clear and descriptive

### Review Process

1. Maintainer reviews the code
2. Feedback is provided if changes are needed
3. Make requested changes in new commits
4. Maintainer approves and merges when ready

## Documentation

### When to Update Docs

- New features should include usage documentation in `docs/`
- Bug fixes affecting behavior should update relevant docs
- API changes should be reflected in XML doc comments

### Documentation Files

| File | Purpose |
|------|---------|
| `README.md` | High-level overview and quick start |
| `docs/installation.md` | Installation and setup instructions |
| `docs/attributes.md` | Attribute reference and options |
| `docs/diagnostics.md` | Analyzer diagnostic codes and messages |
| `docs/patterns.md` | Usage patterns and recipes |

### XML Documentation Comments

Add XML doc comments to public APIs:

```csharp
/// <summary>
/// Maps a source object to a destination object.
/// </summary>
/// <param name="source">The source object to map</param>
/// <returns>The mapped destination object</returns>
/// <remarks>
/// This method is generated at compile time with zero reflection overhead.
/// </remarks>
public static PersonDto ToDto(Person source)
{
    // implementation
}
```

## First-Time Contributor Guide

### Getting Your Feet Wet

New to this project? Here's how to get started:

1. **Set up locally** — Follow [Getting Started](#getting-started)
2. **Explore the codebase** — Spend time reading through the structure in [Project Structure](#project-structure)
3. **Run existing tests** — `dotnet test` to see everything working
4. **Read a snapshot test** — Pick one from `tests/FreakyKit.Forge.Generator.Tests/` to understand test patterns
5. **Ask questions** — Opening discussions or leaving comments is encouraged!

### Finding a Task to Work On

- **Bug fixes** — Look for issues labeled `bug` — these are great entry points
- **Documentation improvements** — Typos, unclear explanations, missing examples
- **Test improvements** — Adding tests for edge cases or missing coverage
- **Small features** — Look for issues labeled `good-first-issue` or `help-wanted`

### Your First Contribution

1. **Pick a small task** — Start with something achievable in 1-2 hours
2. **Create an issue or comment** — Let others know you're working on it
3. **Follow the workflow** — See [Common Workflows](#common-workflows)
4. **Don't worry about perfection** — Maintainers will provide feedback
5. **Ask for help** — Stuck? Leave a comment on your PR or open a discussion

### Common Questions

**Q: Can I contribute without Roslyn experience?**  
A: Yes! Start with documentation, tests, or bug fixes that don't require deep Roslyn knowledge. You'll pick up the concepts as you go.

**Q: What if my PR doesn't get accepted?**  
A: That's okay! Feedback helps you understand the project better. Use it to improve the next attempt.

**Q: How long does review take?**  
A: Depends on complexity. Simple fixes might be reviewed in 1-2 days. Complex features may take longer. Be patient and responsive to feedback.

**Q: Can I work on a large feature as my first contribution?**  
A: Better to start small. Once you're familiar with the codebase and process, large features become much easier.

## Questions and Support

### Getting Help

- **Documentation** — Check [docs/](docs/) for feature guides
- **Examples** — Look at [samples/](samples/) for usage examples
- **Issues** — Search [existing issues](https://github.com/FreakyAli/FreakyKit.Forge/issues)
- **Discussions** — [Start a discussion](https://github.com/FreakyAli/FreakyKit.Forge/discussions) for questions

### Reporting Issues

When reporting a bug:

1. **Search first** — Check existing issues to avoid duplicates
2. **Provide minimal reproduction** — Include a small code example that demonstrates the problem
3. **Include environment info:**
   - .NET version (`dotnet --version`)
   - Forge version
   - OS and any relevant IDE versions
4. **Describe expected vs actual behavior**

### Feature Requests

When requesting a feature:

1. **Explain the use case** — Why is this feature needed?
2. **Provide examples** — Show how the feature would be used
3. **Discuss alternatives** — What's the current workaround?
4. **Consider scope** — Is this a core feature or edge case?

## Additional Resources

- [Forge README](README.md) — Project overview
- [Installation Guide](docs/installation.md) — Setup instructions
- [Attributes Reference](docs/attributes.md) — API documentation
- [Roslyn Documentation](https://github.com/dotnet/roslyn) — Roslyn APIs reference

---

Thank you for contributing to Forge! Your help makes this project better for everyone.
