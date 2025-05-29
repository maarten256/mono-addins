using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Reflection;

namespace Tomboy
{
	public enum Level { DEBUG, INFO, WARN, ERROR, FATAL };

	public interface ILogger
	{
		void Log(Level lvl, CallerContext context, string msg, params object[] args);
	}

	class NullLogger : ILogger
	{
		public void Log(Level lvl, CallerContext context, string msg, params object[] args)
		{
		}
	}

	class ConsoleLogger : ILogger
	{
#if WIN32
		[DllImport("kernel32.dll")]
		public static extern bool AttachConsole (uint dwProcessId);
		const uint ATTACH_PARENT_PROCESS = 0x0ffffffff;
		[DllImport("kernel32.dll")]
		public static extern bool FreeConsole ();

		public ConsoleLogger ()
		{
			AttachConsole (ATTACH_PARENT_PROCESS);
		}

		~ConsoleLogger ()
		{
			FreeConsole ();
		}
#endif

		public void Log(Level lvl, CallerContext context, string msg, params object[] args)
		{
			Console.Write("[{0} {1:00}:{2:00}:{3:00}.{4:000}{5}]",
						   Enum.GetName(typeof(Level), lvl),
						   DateTime.Now.Hour,
						   DateTime.Now.Minute,
						   DateTime.Now.Second,
						   DateTime.Now.Millisecond,
						   context.ToString());
			msg = string.Format(" {0}", msg);
			if (args?.Length > 0)
				Console.WriteLine(msg, args);
			else
				Console.WriteLine(msg);
		}
	}

	class FileLogger : ILogger
	{
		StreamWriter log;
		ConsoleLogger console;

		public FileLogger()
		{
			console = new ConsoleLogger();

			string logDir = Services.NativeApplication.LogDirectory;
			string logfile = Path.Combine(
				logDir,
				"tomboy.log");

			try
			{
				if (!Directory.Exists(logDir))
					Directory.CreateDirectory(logDir);
				log = File.CreateText(logfile);
				log.Flush();
			}
			catch (IOException iox)
			{
				console.Log(Level.WARN,
							new(MethodBase.GetCurrentMethod()?.DeclaringType?.FullName +
								"." + MethodBase.GetCurrentMethod()?.Name),
							"Failed to create the logfile at {0}: {1}",
							logfile, iox.Message);
			}
			catch (UnauthorizedAccessException uax)
			{
				console.Log(Level.WARN,
								new(MethodBase.GetCurrentMethod()?.DeclaringType?.FullName +
									"." + MethodBase.GetCurrentMethod()?.Name),
								"Failed to create the logfile at {0}: {1}",
								logfile, uax.Message);
			}
		}

		~FileLogger()
		{
			if (log != null)
				try
				{
					log.Flush();
				}
				catch { }
		}

		public void Log(Level lvl, CallerContext context, string msg, params object[] args)
		{
			console.Log(lvl, context, msg, args);

			if (log != null)
			{
				msg = string.Format("{0} [{1}]{2}: {3}",
									 DateTime.Now.ToString(),
									 Enum.GetName(typeof(Level), lvl),
									 context.ToString(),
									 msg);
				try
				{
					if (args?.Length > 0)
						log.WriteLine(msg, args);
					else
						log.WriteLine(msg);
					log.Flush();
				}
				catch (IOException iox)
				{
					console.Log(Level.ERROR,
								new(MethodBase.GetCurrentMethod()?.DeclaringType?.FullName +
									"." + MethodBase.GetCurrentMethod()?.Name),
								"Failed to write to the log file due to IO exception: {0}",
								iox.Message);
				}
				catch (Exception ex)
				{
					console.Log(Level.ERROR,
								new(MethodBase.GetCurrentMethod()?.DeclaringType?.FullName +
									"." + MethodBase.GetCurrentMethod()?.Name),
								"Failed to write to the log file due to exception: {0}, stack trace: {1}",
								ex.Message, ex.StackTrace);
				}
			}
		}
	}

	// This class provides a generic logging facility. By default all
	// information is written to standard out and a log file, but other
	// loggers are pluggable.
	public static class Logger
	{
		private static Level log_level = Level.DEBUG;

		static ILogger log_dev = new FileLogger();

		static bool muted = false;

		public static Level LogLevel
		{
			get
			{
				return log_level;
			}
			set
			{
				log_level = value;
			}
		}

		public static ILogger LogDevice
		{
			get
			{
				return log_dev;
			}
			set
			{
				log_dev = value;
			}
		}

		public static void Debug(string msg,
								Gtk.License _ = Gtk.License.Gpl20,
								// This is a hack to allow the use of the [Caller...] attributes
								// without having to pass the parameters explicitly.
								// The _ parameter is never used, but it allows the compiler to
								// automatically fill in the caller information.
								[CallerFilePath] string file = "",
								[CallerLineNumber] int line = 0,
								[CallerMemberName] string member = "")
		{
			Log(Level.DEBUG, new(file, line, member), msg, null);
		}

		public static void Debug(string msg,
								params object[] args)
		{
			Log(Level.DEBUG, new(), msg, args);
		}

		public static void Info(string msg,
			[CallerFilePath] string file = "",
			[CallerLineNumber] int line = 0,
			[CallerMemberName] string member = "")
		{
			Log(Level.INFO, new(file, line, member), msg, null);
		}

		public static void Info(string msg,
			params object[] args)
		{
			Log(Level.INFO, new(), msg, args);
		}

		public static void Warn(string msg,
			[CallerFilePath] string file = "",
			[CallerLineNumber] int line = 0,
			[CallerMemberName] string member = "")
		{
			Log(Level.WARN, new(file, line, member), msg, null);
		}

		public static void Warn(string msg,
			params object[] args)
		{
			Log(Level.WARN, new(), msg, args);
		}

		public static void Error(string msg,
			[CallerFilePath] string file = "",
			[CallerLineNumber] int line = 0,
			[CallerMemberName] string member = "")
		{
			Log(Level.ERROR, new(file, line, member), msg, null);
		}
		public static void Error(string msg,
			params object[] args)
		{
			Log(Level.ERROR, new(), msg, args);
		}

		public static void Fatal(string msg,
			[CallerFilePath] string file = "",
			[CallerLineNumber] int line = 0,
			[CallerMemberName] string member = "")
		{
			Log(Level.FATAL, new(file, line, member), msg, null);
		}

		public static void Fatal(string msg,
		 	params object[] args)
		{
			Log(Level.FATAL, new(), msg, args);
		}

		public static void Log(Level lvl, CallerContext context, string msg, params object[] args)
		{
			if (!muted && lvl >= log_level)
				log_dev.Log(lvl, context, msg, args);
		}

		// This is here to support the original logging, but it should be
		// considered deprecated and old code that uses it should be upgraded to
		// call one of the level specific log methods.
		[Obsolete("Loger.Log is deprecated and should be replaced " +
			"with calls to the level specific log methods")]
		public static void Log(string msg, CallerContext context, params object[] args)
		{
			Log(Level.DEBUG, context, msg, args);
		}

		public static void Mute()
		{
			muted = true;
		}

		public static void Unmute()
		{
			muted = false;
		}
	}

	public readonly struct CallerContext
	{
		public string File { get; }
		public int Line { get; }
		public string Member { get; }

		public CallerContext()
		{
			File = "N/A"; // Optional: shorten file name
			Line = -1;
			Member = "N/A";
		}
		public CallerContext(string file, int line, string member)
		{
			File = System.IO.Path.GetFileName(file); // Optional: shorten file name
			Line = line;
			Member = member;
		}

		public CallerContext(string file)
		{
			File = System.IO.Path.GetFileName(file); // Optional: shorten file name
			Line = -1;
			Member = "N/A";
		}

		public override string ToString()
		{
			if (Line < 0)
				return "";

			return $" {File}:{Line} ({Member})";
		}
	}
}
