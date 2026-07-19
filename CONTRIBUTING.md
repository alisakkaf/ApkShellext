# Contributing to ApkShellext

We welcome contributions of all kinds! Whether you want to fix a bug, suggest a new feature, translate resource strings, or improve the documentation, your help is highly appreciated.

## How to Contribute

### 1. Reporting Issues
* Search existing issues to see if it has already been reported.
* If not, open a new issue describing:
  * Your operating system and hardware architecture (x86/x64/ARM).
  * Clear steps to reproduce the issue.
  * Expected vs. actual behavior.

### 2. Submitting Pull Requests
1. Fork the repository.
2. Create a new feature branch for your changes:
   ```bash
   git checkout -b feature/my-amazing-feature
   ```
3. Make your changes and commit them with descriptive commit messages.
4. Ensure the solution builds successfully without warnings or errors in Release mode.
5. Push to your branch:
   ```bash
   git push origin feature/my-amazing-feature
   ```
6. Open a Pull Request on this repository.

## Development Guidelines
* Target **.NET Framework 4.8** for compatibility.
* Ensure C# source code follows clean code practices and preserves existing localized resx file structures.
* Check that any UI watermark modifications scale dynamically and remain readable on both light and dark backgrounds.
