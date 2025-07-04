using System;
using System.IO;

namespace JuliusSweetland.OptiKey.Services
{
    /// <summary>
    /// Logger for tracking OnboardState and DemoState transitions in the Exhibit interface.
    /// Writes to C:\EyeMineLogs and fails silently if unable to write.
    /// </summary>
    public static class ExhibitStateLogger
    {
        private static readonly string LogDirectory = @"C:\EyeMineLogs";
        private static readonly string LogFileName = "exhibit_state_log.txt";
        private static readonly string LogFilePath = Path.Combine(LogDirectory, LogFileName);
        private static readonly object LockObject = new object();

        /// <summary>
        /// Logs an OnboardState transition
        /// </summary>
        /// <param name="oldState">Previous OnboardState</param>
        /// <param name="newState">New OnboardState</param>
        public static void LogOnboardStateChange(object oldState, object newState)
        {
            LogStateChangeInternal("OnboardState", oldState?.ToString() ?? "null", newState?.ToString() ?? "null");
        }

        /// <summary>
        /// Logs a DemoState transition
        /// </summary>
        /// <param name="oldState">Previous DemoState</param>
        /// <param name="newState">New DemoState</param>
        public static void LogDemoStateChange(object oldState, object newState)
        {
            LogStateChangeInternal("DemoState", oldState?.ToString() ?? "null", newState?.ToString() ?? "null");
        }

        /// <summary>
        /// Logs when a specific state is entered/hit
        /// </summary>
        /// <param name="stateType">Type of state (OnboardState/DemoState)</param>
        /// <param name="stateName">Name of the state</param>
        public static void LogStateHit(string stateType, string stateName)
        {
            LogEntry($"{stateType}_HIT: {stateName}");
        }

        /// <summary>
        /// Logs a generic state change
        /// </summary>
        /// <param name="stateType">Type of state</param>
        /// <param name="oldState">Previous state</param>
        /// <param name="newState">New state</param>
        public static void LogStateChange(string stateType, string oldState, string newState)
        {
            LogStateChangeInternal(stateType, oldState, newState);
        }

        /// <summary>
        /// Internal method to log state changes
        /// </summary>
        private static void LogStateChangeInternal(string stateType, string oldState, string newState)
        {
            if (oldState == newState) return; // Don't log if state didn't actually change
            
            LogEntry($"{stateType}_CHANGE: {oldState} -> {newState}");
        }

        /// <summary>
        /// Core logging method that writes to file with error handling
        /// </summary>
        private static void LogEntry(string message)
        {
            try
            {
                lock (LockObject)
                {
                    // Ensure directory exists
                    if (!Directory.Exists(LogDirectory))
                    {
                        Directory.CreateDirectory(LogDirectory);
                    }

                    // Format log entry with timestamp
                    var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    var logLine = $"[{timestamp}] {message}";

                    // Append to log file
                    File.AppendAllText(LogFilePath, logLine + Environment.NewLine);
                }
            }
            catch
            {
                // Fail silently - logging must not disrupt application functionality
                // No error handling, throwing, or notifications
            }
        }

        /// <summary>
        /// Log session start for tracking purposes
        /// </summary>
        public static void LogSessionStart()
        {
            LogEntry("SESSION_START");
        }

        /// <summary>
        /// Log session end for tracking purposes  
        /// </summary>
        public static void LogSessionEnd()
        {
            LogEntry("SESSION_END");
        }
    }
}