# Essential Coding & Workflow Rules

## 1. Git & Workflow Rules
* **Branching**: Always create and work in a clearly identified branch prefixed with `feature/`, `bug/`, or `doc/` (e.g., `feature/issue-42-description`).
* **Issue Management**: When working on an issue, before starting any work, apply the `in-progress` label to the issue on GitHub.
* **Pull Requests**: Conclude all work by opening a Pull Request targeted to merge into the `v-next` branch.
* **No Auto-Merging**: **NEVER merge a PR or branch.** Merging is strictly the user's responsibility unless explicitly and unambiguously instructed otherwise.

## 2. Coding & Testing Essentials
* **Nullable Reference Types**: Treat all nullable compiler warnings as errors.
* **Smart Unit Testing**: Write smart, meaningful unit tests for any new or modified functionality to verify behavior and prevent regressions.
