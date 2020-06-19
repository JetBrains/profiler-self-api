using System;

namespace JetBrains.Profiler.SelfApi
{
    /// <summary>
    /// Helpers for self-profiling configuration
    /// </summary>
    public static class CommonConfigHelpers
    {
        /// <summary>
        /// Specifies path to log file.
        /// </summary>
        public static T UseLogFile<T>(this T config, string filePath) where T : CommonConfig
        {
            config.LogFile = filePath ?? throw new ArgumentNullException(nameof(filePath));
            return config;
        }

        /// <summary>
        /// [Advanced use only] Specifies whether to use `JetBrains.Profiler.Api` to control the profiling session.
        /// </summary>
        /// <remarks>
        /// By default, `JetBrains.Profiler.Api` is used to control the session (if the corresponding assembly was successfully loaded).
        /// Otherwise, the self-profiling API uses command-line profiler service messages to control the session.
        /// </remarks>
        public static T UseApi<T>(this T config) where T : CommonConfig
        {
            if (config.IsUseApi.HasValue)
                throw new InvalidOperationException("UseApi and DoNotUseApi are mutually exclusive.");
        
            config.IsUseApi = true;
            return config;
        }

        /// <summary>
        /// [Advanced use only] Prohibits using `JetBrains.Profiler.Api` to control the profiling session.
        /// </summary>
        /// <remarks>
        /// By default, `JetBrains.Profiler.Api` is used to control the session (if the corresponding assembly was successfully loaded).
        /// Otherwise, the self-profiling API uses command-line profiler service messages to control the session.
        /// </remarks>
        public static T DoNotUseApi<T>(this T config) where T : CommonConfig
        {
            if (config.IsUseApi.HasValue)
                throw new InvalidOperationException("DoNotUseApi and UseApi are mutually exclusive.");

            config.IsUseApi = false;
            return config;
        }
    }
}