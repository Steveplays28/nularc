# Introduction

> Nularc is a packet-based, lightweight C# UDP networking library.

Nularc is designed to be as lightweight and easy to use as possible, so it will handle the basics of a server and a client for you, while leaving the rest up to you to develop and manage.

If you're wondering about the name, [Nuland](https://en.wikipedia.org/wiki/Nuland) is a place in the Netherlands. Close enough?

## Installation

### NuGet

#### Visual Studio

Install Nularc via the NuGet package manager UI built into Visual Studio.  
For more information, see the [Microsoft Documentation](https://learn.microsoft.com/en-us/nuget/quickstart/install-and-use-a-package-in-visual-studio).

#### .NET CLI

```bash
dotnet add package Nularc
```

For more information, see the [Microsoft Documentation](https://learn.microsoft.com/en-us/nuget/quickstart/install-and-use-a-package-using-the-dotnet-cli#add-the-newtonsoftjson-nuget-package).

### DLL/manual install

Download the [latest release](https://github.com/Steveplays28/nularc/releases/latest), extract it into your project, and add the following to your `.csproj` file (inside the `<Project>` tag):

```cs
<ItemGroup>
  <Reference Include="Nularc">
    <HintPath>PATH\TO\Nularc\Nularc.dll</HintPath>
  </Reference>
</ItemGroup>
```

!> Make sure to change the path to the location of the dll!

## Contributing

If you want to give a suggestion, or add/change something, feel free to [open an issue](https://github.com/Steveplays28/nularc/issues/new)/[create a pull request](https://github.com/Steveplays28/nularc/compare)!  
Please check if there isn't already an issue/pull request open.

### Development environment

Requirements:

- [.NET 6 SDK](https://learn.microsoft.com/en-us/dotnet/core/install/windows?tabs=net60)
- [.NET CLI](https://learn.microsoft.com/en-us/dotnet/core/tools/)

```bash
git clone https://github.com/Steveplays28/nularc.git
cd nularc

dotnet build
```

### Documentation

If you want to edit the documentation, run the following commands to get a local, editable copy of the documentation up and running:

```bash
git clone https://github.com/Steveplays28/nularc.git
cd nularc

docsify serve ./docs
```

You can now preview your changes to the documentation at [localhost:3000](http://localhost:3000).

## License

This project is licensed under the LGPLv2.1 License, see the [LICENSE file](https://github.com/Steveplays28/nexlib/blob/main/LICENSE) for more details.
