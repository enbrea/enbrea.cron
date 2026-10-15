[![NuGet Gallery](https://img.shields.io/badge/NuGet%20Gallery-enbrea.cron-blue.svg)](https://www.nuget.org/packages/Enbrea.Cron/)
![GitHub](https://img.shields.io/github/license/enbrea/enbrea.cron)

# Enbrea.Cron

A .NET library for parsing, matching, and enumerating time-zone-aware cron schedules.

+ Supports `.NET 10`.
+ Parses 5-, 6-, and 7-field cron expressions, including macros such as `@daily` and `@monthly`.
+ Supports cron modifiers such as `?`, `L`, `LW`, `W`, and `#`.
+ Separates cron expressions from schedules, combining a parsed expression with a required time zone.
+ Checks whether a `DateTimeOffset` instant matches a schedule.
+ Computes next and previous occurrences and enumerates occurrences in ascending or descending order.
+ Handles daylight-saving-time transitions by skipping nonexistent local times and selecting the earlier instant for ambiguous local times.
+ Supports local calendar years from `1970` through `2199`.

## Installation

```
dotnet add package Enbrea.Cron
```

## Getting started

Documentation is available in the [GitHub wiki](https://github.com/enbrea/enbrea.cron/wiki).

## Can I help?

Yes, that would be much appreciated. The best way to help is to post a response via the Issue Tracker and/or submit a Pull Request.
