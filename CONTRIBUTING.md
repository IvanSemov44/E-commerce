# Contributing to the E-Commerce Platform

First off, thank you for considering contributing! This project thrives on community involvement, and every contribution is appreciated.

This document provides guidelines for contributing to the project. Please read it carefully to ensure a smooth and effective collaboration process.

## Table of Contents

- [Code of Conduct](#code-of-conduct)
- [How Can I Contribute?](#how-can-i-contribute)
  - [Reporting Bugs](#reporting-bugs)
  - [Suggesting Enhancements](#suggesting-enhancements)
  - [Submitting Pull Requests](#submitting-pull-requests)
- [Development Workflow](#development-workflow)
  - [Prerequisites](#prerequisites)
  - [Branching](#branching)
  - [Coding Conventions](#coding-conventions)
- [Pull Request Process](#pull-request-process)

## Code of Conduct

This project and everyone participating in it is governed by a [Code of Conduct](CODE_OF_CONDUCT.md). By participating, you are expected to uphold this code. Please report unacceptable behavior. (Note: `CODE_OF_CONDUCT.md` to be created).

## How Can I Contribute?

### Reporting Bugs

If you find a bug, please ensure the bug was not already reported by searching on GitHub under [Issues](https://github.com/your-repo/issues). If you're unable to find an open issue addressing the problem, open a new one. Be sure to include a **title and clear description**, as much relevant information as possible, and a **code sample or an executable test case** demonstrating the expected behavior that is not occurring.

### Suggesting Enhancements

If you have an idea for an enhancement, please open an issue to discuss it. This allows us to coordinate efforts and ensure the proposal aligns with the project's goals.

### Submitting Pull Requests

We love pull requests! Please see the sections below on the development workflow and pull request process before you start.

## Development Workflow

### Prerequisites

-   Ensure you have followed the setup guide in the [README.md](./README.md) to get your local environment running.
-   You have a GitHub account.

### Branching

We use a branching model based on GitFlow. All development happens in feature branches.

1.  **Fork the repository** on GitHub.
2.  **Clone your fork** locally: `git clone https://github.com/your-username/E-commerce.git`
3.  **Create a new feature branch** from the `main` branch:
    ```sh
    # Example for a new feature
    git checkout -b feature/amazing-new-feature

    # Example for a bug fix
    git checkout -b fix/bug-in-cart
    ```
    Use a descriptive name for your branch (e.g., `feature/user-profile-page`, `fix/payment-gateway-error`).

### Coding Conventions

-   **TypeScript/Frontend:** We use ESLint to enforce code style. Run `npm run lint` in the `admin` and `storefront` directories to check your code.
-   **C#/Backend:** Please follow the standard .NET/C# coding conventions. Most rules are enforced by modern IDEs like Visual Studio and JetBrains Rider.
-   **Commit Messages:** Write clear and concise commit messages. The first line should be a short summary (50 characters or less), followed by a blank line and a more detailed explanation if necessary.

## Pull Request Process

1.  Ensure your code adheres to the **Coding Conventions**.
2.  Make sure all existing **tests pass**. Add new tests for your feature or bug fix. Run tests with `dotnet test` in the `src/backend` directory.
3.  Update the [README.md](./README.md) or other relevant documentation if your changes impact the project's setup, environment variables, or architecture.
4.  Push your feature branch to your fork on GitHub.
5.  Open a pull request from your feature branch to the `main` branch of the original repository.
6.  In the pull request description, clearly explain the **purpose** and **scope** of your changes. Link to any relevant issues.
7.  The pull request will be reviewed by maintainers. Address any feedback or requested changes.

Once your PR is approved and merges, your contribution will be part of the project. Thank you for your hard work!
