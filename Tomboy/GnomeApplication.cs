using System;
using System.Runtime.InteropServices;
using System.Text;
using System.IO;

using Hyena;

using System.Threading.Tasks;
using Tmds.DBus;

namespace Tomboy
{
    public class GnomeApplication : INativeApplication
    {
        private Connection _connection;
        private ISessionManager _sessionManager;
        private IClientPrivate _clientPrivate;

        private IDisposable _stopSubscription;
        private IDisposable _endSessionSubscription;
        private IDisposable _queryEndSessionSubscription;
        private IDisposable _cancelEndSessionSubscription;

		private static string confDir;
		private static string dataDir;
		private static string cacheDir;
        private static string tomboyDirName = "tomboy";
        private ObjectPath _sessionClientId;

		static GnomeApplication ()
		{
			Console.WriteLine ("In GnomeApplication(): Creating GnomeApplication...");
			dataDir = Path.Combine (XdgBaseDirectorySpec.GetUserDirectory ("XDG_DATA_HOME",
			                                                               Path.Combine (".local", "share")),
			                        tomboyDirName);
			confDir = Path.Combine (XdgBaseDirectorySpec.GetUserDirectory ("XDG_CONFIG_HOME",
			                                                               ".config"),
			                        tomboyDirName);
			cacheDir = Path.Combine (XdgBaseDirectorySpec.GetUserDirectory ("XDG_CACHE_HOME",
			                                                                ".cache"),
			                         tomboyDirName);

			// NOTE: Other directories created on demand
			//       (non-existence is an indicator that migration is needed)
			if (!Directory.Exists (cacheDir))
				Directory.CreateDirectory (cacheDir);
		}

		public async Task Initialize(string locale_dir,
								string display_name,
								string process_name,
								string[] args)
		{
			try
			{
				SetProcessName(process_name);
			}
			catch { } // Ignore exception if fail (not needed to run)

			Logger.Debug("In GnomeApplication::Initialize(): Initializing GnomeApplication with display name: {0}", display_name);
			await InitializeAsync(display_name);

			Logger.Debug("In GnomeApplication::Initialize(): Initializing Gtk");
			Gtk.Application.Init();
			Logger.Debug("In GnomeApplication::Initialize(): Gtk initialized");
        }

        public void RegisterSessionManagerRestart (string executable_path,
		                string[] args,
		                string[] environment)
		{
			// Nothing to do, we dropped the .desktop file in the autostart
			// folder which should be enough to handle this in Gnome
		}

        public void RegisterSignalHandlers ()
		{
			// Connect to SIGTERM and SIGINT using UnixSignal, so we don't lose
			// unsaved notes on exit...
			Task.Run(() =>
			{
				var sigterm = new Mono.Unix.UnixSignal(Mono.Unix.Native.Signum.SIGTERM);
				var sigint = new Mono.Unix.UnixSignal(Mono.Unix.Native.Signum.SIGINT);
				while (true)
				{
					int index = Mono.Unix.UnixSignal.WaitAny(new[] { sigterm, sigint });
					OnExitSignal(-1);
				}
			});
		}

        public event EventHandler ExitingEvent;

		public void Exit (int exitcode)
		{
			OnExitSignal (-1);
			System.Environment.Exit (exitcode);
		}

		public void StartMainLoop ()
		{
			Logger.Debug ("Kicking off Gtk main loop...");
			Gtk.Application.Run ();
		}

        public string DataDirectory {
			get { return dataDir; }
		}

		public void DisplayHelp (string project, string page, Gdk.Screen screen)
		{
			string helpUrl = string.Format("http://library.gnome.org/users/{0}/", project);

			var langsPtr = g_get_language_names ();
			var langs = GLib.Marshaller.NullTermPtrToStringArray (langsPtr, false);
			var baseHelpDir = Path.Combine (Path.Combine (Defines.DATADIR, "gnome/help"), project);
			if (Directory.Exists (baseHelpDir)) {
				foreach (var lang in langs) {
					var langHelpDir = Path.Combine (baseHelpDir, lang);
					if (Directory.Exists (langHelpDir))
						// TODO:Support page
						helpUrl = String.Format ("ghelp://{0}", langHelpDir);
				}
			}

			OpenUrl (helpUrl, screen);
		}

        public void OpenUrl(string url, Gdk.Screen screen)
        {
            GtkBeans.Global.ShowUri(screen, url);
        }

        public string ConfigurationDirectory
        {
            get { return confDir; }
        }

		public string CacheDirectory {
			get { return cacheDir; }
		}

		public string LogDirectory {
			get { return confDir; }
		}

		public string PreOneDotZeroNoteDirectory {
			get {
				return Path.Combine (Environment.GetEnvironmentVariable ("HOME"),
				                     ".tomboy");
			}
		}

		[DllImport ("glib-2.0.dll")]
		static extern IntPtr g_get_language_names ();

        [DllImport("libc")]
		private static extern int prctl (int option,
			                                 byte [] arg2,
			                                 IntPtr arg3,
			                                 IntPtr arg4,
			                                 IntPtr arg5);

		private static void SetProcessName (string name)
		{
			if (prctl (15 /* PR_SET_NAME */,
			                Encoding.ASCII.GetBytes (name + "\0"),
			                IntPtr.Zero,
			                IntPtr.Zero,
			                IntPtr.Zero) != 0)
				throw new ApplicationException (
				        "Error setting process name: " +
				        Mono.Unix.Native.Stdlib.GetLastError ());
		}

        private void OnExitSignal(int signal)
        {
            if (ExitingEvent != null)
                ExitingEvent(null, new EventArgs());

            if (signal >= 0)
                System.Environment.Exit(0);
        }

		private void OnStop () {
			Exit(0);
		}

        private async void OnQueryEndSession(uint flags)
        {
            Logger.Info("Received end session query");

            try {
                if (_clientPrivate != null)
                    await _clientPrivate.EndSessionResponseAsync(true, string.Empty);
            } catch (Exception e) {
                Logger.Debug("Failed to respond to session manager: {0}", e.Message);
            }
        }

		private async void OnEndSession (uint flags)
		{
			Logger.Info ("Received end session signal");

			if (ExitingEvent != null)
				ExitingEvent (null, new EventArgs ());

			// Let the session manager know its OK to continue
			// Ideally we would wait for all the exit events to finish
			try {
                if (_clientPrivate != null)
                    await _clientPrivate.EndSessionResponseAsync(true, string.Empty);
			} catch (Exception e) {
				Logger.Debug ("Failed to respond to session manager: {0}", e.Message);
			}
			Exit (0);
		}

        private async Task InitializeAsync(string displayName)
        {
			Logger.Debug("In GnomeApplication::InitializeAsync()");
            _connection = Connection.Session;
            _sessionManager = _connection.CreateProxy<ISessionManager>(
                Constants.SessionManagerInterfaceName,
                new ObjectPath(Constants.SessionManagerPath));

            string startupId = Environment.GetEnvironmentVariable("DESKTOP_AUTOSTART_ID") ?? displayName;

            try
            {
                // Register client and get ObjectPath for session client
                _sessionClientId = await _sessionManager.RegisterClientAsync(displayName, startupId);

                // Create ClientPrivate proxy
                _clientPrivate = _connection.CreateProxy<IClientPrivate>(
                    Constants.SessionManagerInterfaceName,
                    _sessionClientId);

                // Subscribe to signals
                _stopSubscription = await _clientPrivate.WatchStopAsync(OnStop);
                _endSessionSubscription = await _clientPrivate.WatchEndSessionAsync(OnEndSession);
                _queryEndSessionSubscription = await _clientPrivate.WatchQueryEndSessionAsync(OnQueryEndSession);
            }
            catch (Exception e)
            {
                Logger.Debug($"Failed to register with session manager: {e.Message}");
            }
        }

        public void Dispose()
        {
            _stopSubscription?.Dispose();
            _endSessionSubscription?.Dispose();
            _queryEndSessionSubscription?.Dispose();
            _cancelEndSessionSubscription?.Dispose();
            _connection?.Dispose();
        }
    }
}