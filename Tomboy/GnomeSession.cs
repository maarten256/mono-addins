// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//
// Copyright (c) 2010 Aaron Borden <adborden@live.com>
//

using System;
using System.Threading.Tasks;
using Tmds.DBus;

// Gnome Session DBus API
// http://people.gnome.org/~mccann/gnome-session/docs/gnome-session.html
// Here we've defined only what's needed to register and respond to the
// SessionManager
namespace org.gnome.SessionManager
{
    public static class Constants
    {
        public const string SessionManagerPath = "/org/gnome/SessionManager";
        public const string SessionManagerInterfaceName = "org.gnome.SessionManager";
        public const string ClientPrivateInterfaceName = "org.gnome.SessionManager.ClientPrivate";
    }

    [DBusInterface(Constants.SessionManagerInterfaceName)]
    public interface ISessionManager : IDBusObject
    {
        Task SetenvAsync(string variable, string val);
        Task InitializationErrorAsync(string message, bool fatal);
        Task<ObjectPath> RegisterClientAsync(string app_id, string client_startup_id);
        Task UnregisterClientAsync(ObjectPath client_id);
    }

    // Delegate signatures for events
    public delegate void StopHandler();
    public delegate void QueryEndSessionHandler(uint flags);
    public delegate void EndSessionHandler(uint flags);
    public delegate void CancelEndSessionHandler();

    [DBusInterface(Constants.ClientPrivateInterfaceName)]
    public interface IClientPrivate : IDBusObject
    {
        Task EndSessionResponseAsync(bool is_ok, string reason);

        // Signals as events (Tmds.DBus signals return IDisposable for subscriptions)
        Task<IDisposable> WatchStopAsync(Action handler);
        Task<IDisposable> WatchEndSessionAsync(Action<uint> handler);
        Task<IDisposable> WatchQueryEndSessionAsync(Action<uint> handler);
    }
}
