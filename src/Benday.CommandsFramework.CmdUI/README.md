# cmdui

A Blazor Server web UI shell for any CLI tool built with [Benday.CommandsFramework](https://www.nuget.org/packages/Benday.CommandsFramework/).

## Features

- Auto-generates a web-based UI from any CommandsFramework CLI tool
- Discovers all installed CommandsFramework tools when run without arguments
- Form-based command execution with validation
- Real-time output display
- Working directory selection

## Installation

```bash
dotnet tool install -g cmdui
```

## Usage

### Launch UI for a specific tool
```bash
cmdui slnutil
```

### Discover all installed tools
```bash
cmdui
```

### Launch from within a tool
Any CommandsFramework-based tool can launch cmdui directly:
```bash
slnutil gui
```

## About

Written by Benjamin Day
Pluralsight Author | Microsoft MVP | Scrum.org Professional Scrum Trainer
https://www.benday.com
info@benday.com

[Source code](https://github.com/benday-inc/Benday.CommandsFramework)
