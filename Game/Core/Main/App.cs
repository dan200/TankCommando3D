using Dan200.Core.Assets;
using Dan200.Core.Audio.Null;
using Dan200.Core.Audio.OpenAL;
using Dan200.Core.Render;
using Dan200.Core.Script;
using Dan200.Core.Util;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Dan200.Core.Platform;
using System.Diagnostics;

namespace Dan200.Core.Main
{
    internal enum LogLevel
    {
        Debug,
        Info,
        Warning,
        Error
    }

	internal struct LogEntry
	{
		public readonly LogLevel Level;
		public readonly string Text;

		public LogEntry(LogLevel level, string text)
		{
			Level = level;
			Text = text;
		}
	}

	internal class App
	{
        public const int MAX_FPS = 250;
		public const int MAX_LOG_FILES = 10;
		public const int RECENT_LOG_SIZE = 40;

#if DEBUG
		public static readonly bool Debug = true;
#if STEAM
        public static readonly bool Steam = true;
#else
		public static readonly bool Steam = false;
#endif
#else
		public const bool Debug = false;
#if STEAM
        public const bool Steam = true;
#else
		public const bool Steam = false;
#endif
#endif

        public static float FPS
        {
            get;
            set;
        }

        public static ProgramArguments Arguments
        {
            get;
            private set;
        }

        public static GameInfo Info
        {
            get;
            private set;
        }

		public static IPlatform Platform
		{
			get;
			set;
		} = null;

		public static string SavePath
		{
			get;
			set;
		} = ".";

		public static string AssetPath
		{
			get;
			set;
		} = "assets";

        public static DebugDraw DebugDraw
        {
            get;
            set;
        }

		private static TextWriter s_logFile = null;
        private static string s_logFilePath = null;
		private static Queue<LogEntry> s_recentLog = new Queue<LogEntry>(RECENT_LOG_SIZE);
        private static event StaticStructEventHandler<LogEntry> s_onLog;

        public static event StaticStructEventHandler<LogEntry> OnLog
        {
            add
            {
                lock(s_recentLog)
                {
                    s_onLog += value;
                }
            }
            remove
            {
                lock(s_recentLog)
                {
                    s_onLog -= value;
                }
            }
        }

		public static LogEntry[] RecentLog
        {
            get
            {
                lock(s_recentLog)
                {
                    return s_recentLog.ToArray();
                }
            }
        }

        public static void Log(LogLevel level, string text)
        {
            // Discard debug
			if (!App.Debug && level == LogLevel.Debug)
            {
                return;
            }

			// Add the entry to recent error log list
			var entry = new LogEntry(level, text);
            lock (s_recentLog)
            {
                while (s_recentLog.Count >= RECENT_LOG_SIZE)
                {
                    s_recentLog.Dequeue();
                }
                s_recentLog.Enqueue(entry);
            }

			// Emit the log to anybody interested
            var onLog = s_onLog;
			if (onLog != null)
			{
				onLog.Invoke(entry);
			}
		}

		[Conditional("DEBUG")]
		public static void LogDebug(string format, params object[] args)
		{
			Log(LogLevel.Debug, string.Format(format, args));
		}

		public static void Log(string format, params object[] args)
		{
			Log(LogLevel.Info, string.Format(format, args));
		}

        public static void LogWarning(string format, params object[] args)
        {
            Log(LogLevel.Warning, string.Format(format, args));
        }

        public static void LogError(string format, params object[] args)
		{
			Log(LogLevel.Error, string.Format(format, args));
		}

		[Conditional("DEBUG")]
		public static void Assert(bool condition, string message = null, [CallerFilePathAttribute]string filePath = null, [CallerLineNumber]int lineNumber = 0)
		{
			if (!condition)
			{
				// The condition failed
				if (message == null)
				{
					// Build an error message if none is provided
					if (filePath != null)
					{
						// Try using the assert condition from the source code as the message
						if (App.Debug && File.Exists(filePath))
						{
							string[] lines = File.ReadAllLines(filePath);
							if (lineNumber <= lines.Length)
							{
								string line = lines[lineNumber - 1];
								int lineStartIdx = line.IndexOf("Assert", StringComparison.InvariantCulture);
								int lineEndIdx = (lineStartIdx >= 0) ? line.IndexOf(";", lineStartIdx, StringComparison.InvariantCulture) : -1;
								if (lineStartIdx >= 0 && lineEndIdx >= 0 && lineEndIdx > lineStartIdx)
								{
									int conditionStartIdx = line.IndexOf("(", lineStartIdx, lineEndIdx - lineStartIdx, StringComparison.InvariantCulture);
									int conditionEndIdx = line.LastIndexOf(")", lineEndIdx, lineEndIdx - lineStartIdx, StringComparison.InvariantCulture);
									if (conditionStartIdx >= 0 && conditionEndIdx >= 0 && conditionEndIdx > conditionStartIdx)
									{
										string conditionString = line.Substring(conditionStartIdx + 1, conditionEndIdx - (conditionStartIdx + 1)).Trim();
										message = "Assertion failed: " + conditionString;
									}
								}
							}
						}

						// Just use the file location otherwise
						if (message == null)
						{
							message = "Assertion failed: " + Path.GetFileName(filePath) + ":" + lineNumber;
						}
					}
					else
					{
						// No file path sepcified. Use a default message 
						message = "Assertion failed";
					}
				}
                if(App.Debug)
                {
                    throw new AssertionFailedException(message);
                }
                else
                {
                    App.LogError(message);
                }
            }
		}

		public static Exception Rethrow(Exception e) // Never actually returns, but allows callers to write "throw App.Rethrow()" to keep the compiler happy
		{
			var info = System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(e);
			info.Throw();
			throw e; // Unreachable, but we need to throw/return something
		}

		public static void StartLogging()
        {
			// Setup logging
			App.Assert(s_logFile == null);
            try
            {
                // Build the path
                string logFilePath = Path.Combine(SavePath, "logs");
                logFilePath = Path.Combine(logFilePath, DateTime.Now.ToString("s").Replace(":", "-") + ".txt");

                // Prepare the directory
                var logDirectory = Path.GetDirectoryName(logFilePath);
                if (!Directory.Exists(logDirectory))
                {
                    // Create the log file directory
                    Directory.CreateDirectory(logDirectory);
                }
                else
                {
                    // Delete old log files from the directory
                    var directoryInfo = new DirectoryInfo(logDirectory);
                    var oldFiles = directoryInfo.EnumerateFiles()
                        .Where(file => file.Extension == ".txt")
                        .OrderByDescending(file => file.CreationTime)
                        .Skip(MAX_LOG_FILES - 1);
                    foreach (var file in oldFiles.ToList())
                    {
                        file.Delete();
                    }
                }

                // Open the log file, log early messages
                App.Log("Logging to {0}", logFilePath);
                var logFile = new StreamWriter(logFilePath);
                lock (s_recentLog)
                {
                    foreach (var entry in s_recentLog)
                    {
                        logFile.WriteLine(entry.Text);
                    }
                    logFile.Flush();
                }
                s_logFile = logFile;
                s_logFilePath = logFilePath;
				App.OnLog += OnFileLog;
			}
			catch (IOException)
			{
				App.LogError("Failed to open log file");
			}
		}

		private static void OnFileLog(LogEntry entry)
		{
            App.Assert(s_logFile != null);
            lock(s_logFile)
            {
                s_logFile.WriteLine(entry.Text);
                s_logFile.Flush();
            }
		}

		private static void OnConsoleLog(LogEntry entry)
		{
			if (App.Debug)
			{
				System.Diagnostics.Debug.WriteLine(entry.Text);
				System.Diagnostics.Debug.Flush();
			}
			else
			{
                Console.WriteLine(entry.Text);
                Console.Out.Flush();
			}
		}

        private static bool m_odeInitialised;
        
        public static void Init(GameInfo gameInfo, ProgramArguments arguments)
        {
			App.Assert(App.Platform != null, "App.Init called twice");

			// Start logging
			App.OnLog += OnConsoleLog;

			// Store info and commandline arguments
			App.Info = gameInfo;
            App.Arguments = arguments;

            // Print App Info
            if (App.Debug && App.Steam)
            {
                App.Log("{0} {1} (Steam Debug build)", Info.Title, Info.Version);
            }
            else if (App.Debug)
            {
                App.Log("{0} {1} (Debug build)", Info.Title, Info.Version);
            }
            else if (App.Steam)
            {
                App.Log("{0} {1} (Steam build)", Info.Title, Info.Version);
            }
            else
            {
                App.Log("{0} {1}", Info.Title, Info.Version);
            }
            if (!App.Arguments.IsEmpty)
            {
				App.Log("Command Line Arguments: {0}", App.Arguments.ToString());
            }
            App.Log("Developed by {0}", App.Info.Author);
            App.Log("Platform: {0}", App.Platform.Type);

            // Init ODE
            ODE.ODE.dInitODE();
            m_odeInitialised = true;
        }

        public static void HandleError(Exception e)
        {
            // Log error to console
            var message = string.Format("Game crashed with {0}: {1}", e.GetType().FullName, e.Message);
			App.LogError(message);
            App.LogError(e.StackTrace);
            while ((e = e.InnerException) != null)
            {
                App.LogError("Caused by {0}: {1}", e.GetType().FullName, e.Message);
                App.LogError(e.StackTrace);
            }

            // Open an emergency log file if necessary
            if (s_logFile == null)
            {
                var logFilePath = "log.txt";
                try
                {
                    var logFile = new StreamWriter("log.txt");
                    App.Log("Logging to {0}", logFilePath);
                    lock (s_recentLog)
                    {
                        foreach (var entry in s_recentLog)
                        {
                            logFile.WriteLine(entry.Text);
                        }
                    }
                    logFile.Flush();
                    s_logFile = logFile;
                    s_logFilePath = logFilePath;
                    App.OnLog += OnFileLog;
                }
                catch (IOException)
                {
					App.LogError("Failed to open {0}", logFilePath);
                }
            }

            // Pop up the message box
            if (s_logFilePath != null)
            {
                message += Environment.NewLine;
                message += Environment.NewLine;
                message += "Callstack written to " + s_logFilePath;
            }
			App.Platform.ShowMessageBox(
				Info.Title + " " + Info.Version,
				message,
				true
			);
        }

        public static void Shutdown()
        {
            // Shutdown ODE
            if (m_odeInitialised)
            {
                ODE.ODE.dCloseODE();
                m_odeInitialised = false;
            }

            // Close the log file
            var file = s_logFile;
            if (file != null)
            {
                App.Log("Closing log file");
                lock(s_logFile)
                {
                    App.OnLog -= OnFileLog;
                    s_logFile.Dispose();
                }
				s_logFile = null;
                s_logFilePath = null;
                App.Log("Log file closed");
            }

            // Quit
            App.Log("Quitting");
            lock (s_recentLog)
            {
                s_recentLog.Clear();
                s_onLog = null;
            }
        }
    }
}
