# JetBrains.Profiler.SelfApi [![official JetBrains project](https://jb.gg/badges/official.svg)](https://confluence.jetbrains.com/display/ALL/JetBrains+on+GitHub)

[![net](https://github.com/JetBrains/profiler-self-api/actions/workflows/build-and-test.yml/badge.svg)](https://github.com/JetBrains/profiler-self-api/actions/workflows/build-and-test.yml)

[![NuGet Version](https://img.shields.io/nuget/v/JetBrains.Profiler.SelfApi?label=JetBrains.Profiler.SelfApi)](https://www.nuget.org/packages/JetBrains.Profiler.SelfApi)

JetBrains Self-Profiling API lets you initiate and control profiling sessions right from the code of your application.
The snapshots taken by the API calls can be later opened and investigated in JetBrains profiling tools.
The main advantage of Self-Profiling API is that it doesn't require the profiling tools to be installed on the end-user machine.
For example, you can use this API to take memory snapshots of your application on end-user desktops, staging and production servers, and so on.
For the details on how to use the API, refer to:
- [dotTrace online documentation](https://www.jetbrains.com/help/profiler/Profiling_Guidelines__Advanced_Profiling_Using_dotTrace_API.html#self-profiled-applications)
- [dotMemory online documentation](https://www.jetbrains.com/help/dotmemory/Profiling_Guidelines__Advanced_Profiling_Using_dotTrace_API.html#self-profiled-applications)
