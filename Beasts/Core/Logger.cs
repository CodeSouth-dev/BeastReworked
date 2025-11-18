using System;
using System.Runtime.CompilerServices;
using log4net;

namespace Beasts.Core
{
    /// <summary>
    /// Logger utility class for getting log4net loggers
    /// </summary>
    public static class Logger
    {
        /// <summary>
        /// Gets a logger instance for the calling type
        /// </summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static ILog GetLoggerInstanceForType()
        {
            var frame = new System.Diagnostics.StackFrame(1, false);
            var method = frame.GetMethod();
            var type = method.DeclaringType;
            return LogManager.GetLogger(type);
        }
    }
}
