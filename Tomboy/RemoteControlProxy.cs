using System;
using System.Threading.Tasks;
using Tmds.DBus;

namespace Tomboy
{
    public static class RemoteControlProxy
    {
        private const string Path = "/org/gnome/Tomboy/RemoteControl";
        private const string Namespace = "org.gnome.Tomboy";

        private static bool? firstInstance;
        private static Connection _sessionBus;
        private static IRemoteControl _remoteControlProxy;

        public static async Task<IRemoteControl> GetInstanceAsync()
        {
            await EnsureBusInitialized();

            if (_remoteControlProxy == null)
            {
                _remoteControlProxy = _sessionBus.CreateProxy<IRemoteControl>(Namespace, Path);
            }

            return _remoteControlProxy;
        }

        public static async Task<RemoteControl> RegisterAsync(NoteManager manager)
        {
            if (!await FirstInstanceAsync())
                return null;

            var remoteControl = new RemoteControl(manager, Path);
            await _sessionBus.RegisterObjectAsync(remoteControl);
            return remoteControl;
        }

        public static async Task<bool> FirstInstanceAsync()
        {
            await EnsureBusInitialized();

            if (!firstInstance.HasValue)
            {
                var dbus = _sessionBus.CreateProxy<IDBus>("org.freedesktop.DBus", "/org/freedesktop/DBus");

                bool hasOwner = await dbus.NameHasOwnerAsync(Namespace);
                if (!hasOwner)
                {
                    var reply = await dbus.RequestNameAsync(Namespace, RequestNameFlags.None);
                    firstInstance = reply == RequestNameReply.PrimaryOwner;
                }
                else
                {
                    firstInstance = false;
                }
            }

            return firstInstance.Value;
        }

        private static async Task EnsureBusInitialized()
        {
            if (_sessionBus == null)
            {
                _sessionBus = new Connection(Address.Session);
                await _sessionBus.ConnectAsync();
            }
        }
    }
    

    [DBusInterface("org.freedesktop.DBus")]
    public interface IDBus : IDBusObject
    {
        Task<bool> NameHasOwnerAsync(string name);
        Task<RequestNameReply> RequestNameAsync(string name, RequestNameFlags flags);
    }

    public enum RequestNameReply
    {
        PrimaryOwner = 1,
        InQueue = 2,
        Exists = 3,
        AlreadyOwner = 4
    }

    [Flags]
    public enum RequestNameFlags : uint
    {
        None = 0,
        AllowReplacement = 1,
        ReplaceExisting = 2,
        DoNotQueue = 4
    }
}