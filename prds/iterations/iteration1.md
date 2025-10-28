### **Iteration 1: Project Scaffolding & Foundational Setup**

**Goal:** Establish the complete directory structure, solution, and projects. Configure the development environment.

1.  **Create the Root Directory:**
    *   Create a new directory named `onboard-pro`.

2.  **Initialize .NET Solution and Projects:**
    *   Inside `onboard-pro`, create the solution file:
        ```bash
        dotnet new sln -n Onboard
        ```
    *   Create the `src` directory.
    *   Create the main console application project:
        ```bash
        dotnet new console -n Onboard.Console -o src/Onboard.Console
        ```
    *   Create the core logic class library:
        ```bash
        dotnet new classlib -n Onboard.Core -o src/Onboard.Core
        ```
    *   Create the unit test project:
        ```bash
        dotnet new nunit -n Onboard.Core.Tests -o tests/Onboard.Core.Tests
        ```

3.  **Link Projects to Solution:**
    *   Add all three projects to the solution:
        ```bash
        dotnet sln add src/Onboard.Console/Onboard.Console.csproj
        dotnet sln add src/Onboard.Core/Onboard.Core.csproj
        dotnet sln add tests/Onboard.Core.Tests/Onboard.Core.Tests.csproj
        ```

4.  **Establish Project References:**
    *   `Onboard.Console` must reference `Onboard.Core`:
        ```bash
        dotnet add src/Onboard.Console/Onboard.Console.csproj reference src/Onboard.Core/Onboard.Core.csproj
        ```
    *   `Onboard.Core.Tests` must reference `Onboard.Core`:
        ```bash
        dotnet add tests/Onboard.Core.Tests/Onboard.Core.Tests.csproj reference src/Onboard.Core/Onboard.Core.csproj
        ```

5.  **Install NuGet Packages:**
    *   For `Onboard.Console`:
        ```bash
        dotnet add src/Onboard.Console/Onboard.Console.csproj package Microsoft.Extensions.Hosting
        ```
    *   For `Onboard.Core.Tests`:
        ```bash
        dotnet add tests/Onboard.Core.Tests/Onboard.Core.Tests.csproj package Moq
        ```
        *(NUnit and the Test SDK are already included by the `nunit` template).*

6.  **Create Directory Structure:**
    *   Create the directory structure specified in the design document within the `src` and `tests` directories. You can use `mkdir -p`. At the end of this step, the file system should match the layout in section 3.1 of the design document, even if the files are empty.

7.  **Configure Dev Container:**
    *   Create the `.devcontainer/devcontainer.json` file.
    *   Configure it to use the `mcr.microsoft.com/devcontainers/dotnet:1-9.0-bookworm` image as specified.
