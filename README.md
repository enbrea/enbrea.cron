[![NuGet Enbrea.Cron](https://img.shields.io/nuget/v/Enbrea.Cron?label=Enbrea.Cron)](https://www.nuget.org/packages/Enbrea.Cron/)
[![NuGet Enbrea.Cron.Descriptor](https://img.shields.io/nuget/v/Enbrea.Cron.Descriptor?label=Enbrea.Cron.Descriptor)](https://www.nuget.org/packages/Enbrea.Cron.Descriptor/)
![GitHub](https://img.shields.io/github/license/enbrea/enbrea.cron)

# Enbrea.Cron

A .NET library for parsing, matching, and enumerating time-zone-aware cron schedules.

This repository contains two packages:

+ [`Enbrea.Cron`](https://www.nuget.org/packages/Enbrea.Cron) provides cron expression parsing, schedule matching, and occurrence enumeration.
+ [`Enbrea.Cron.Descriptor`](https://www.nuget.org/packages/Enbrea.Cron.Descriptor) extends the core library with human-readable descriptions of cron expressions and schedules in multiple languages.

## Enbrea.Cron

+ Supports `.NET 10`.
+ Parses 5-, 6-, and 7-field cron expressions, including macros such as `@daily`, `@monthly`, `@weekdays`, and `@weekends`.
+ Supports cron modifiers such as `?`, `L`, `LW`, `W`, and `#`.
+ Separates cron expressions from schedules, combining a parsed expression with a required time zone.
+ Checks whether a `DateTimeOffset` instant matches a schedule.
+ Computes next and previous occurrences and enumerates occurrences in ascending or descending order.
+ Handles daylight-saving-time transitions by skipping nonexistent local times and selecting the earlier instant for ambiguous local times.
+ Supports local calendar years from `1970` through `2199`.
+ Has zero external dependencies.

## Enbrea.Cron.Descriptor

+ Depends on `Enbrea.Cron`.
+ Produces human-readable descriptions of cron expressions and schedules via `.Describe()` extension methods.
+ Supports currently 9 languages: English, German, French, Italian, Portuguese, Russian, Spanish, Turkish, and Ukrainian.
+ Accepts a two-letter ISO 639-1 language code; unknown codes fall back to English.
+ Optionally accepts a UTC reference time for relative next/previous occurrence phrasing.

## Installation

Install the core package:

```
dotnet add package Enbrea.Cron
```

For human-readable descriptions, also install the descriptor package:

```
dotnet add package Enbrea.Cron.Descriptor
```

`Enbrea.Cron.Descriptor` depends on `Enbrea.Cron`, so installing it automatically includes the core package.

## Getting started

Documentation is available in the [GitHub wiki](https://github.com/enbrea/enbrea.cron/wiki).

## Can I help?

Yes, that would be much appreciated. The best way to help is to post a response via the Issue Tracker and/or submit a Pull Request.
