// ---------------------------------------------------------------------------------
// Copyright (c) 2025 Maarten Jacobs
// All rights reserved.
// This file is part of tomboy-modern and was created specifically for this project.
// ---------------------------------------------------------------------------------
using System;
using System.Runtime.InteropServices;
using Gtk;
using Gdk;

namespace Tomboy.Compat
{
    public class CompatTrayIcon : GLib.Object
    {
        private readonly StatusIcon legacyIcon;

        public CompatTrayIcon()
        {
            legacyIcon = new StatusIcon
            {
                Visible = true
            };

            legacyIcon.Activate += (s, e) => OnActivate();

            // Manually connect to the native "popup-menu" signal
            GObjectInterop.g_signal_connect_data(
                legacyIcon.Handle,
                "popup-menu",
                new PopupMenuNativeDelegate(PopupMenuSignalHandler),
                IntPtr.Zero,
                IntPtr.Zero,
                GObjectInterop.GConnectFlags.None
            );
        }

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        private delegate void PopupMenuNativeDelegate(IntPtr statusIcon, uint button, uint activate_time);

        private void PopupMenuSignalHandler(IntPtr statusIcon, uint button, uint activate_time)
        {
            OnPopupMenu(button, activate_time);
        }

        protected virtual void OnActivate()
        {
            // Override in subclass
        }

        protected virtual void OnPopupMenu(uint button, uint activate_time)
        {
            // Override in subclass
        }

        // Emulates Gtk.StatusIcon.Pixbuf
        public Pixbuf Pixbuf
        {
            get => legacyIcon.Pixbuf;
            set => legacyIcon.Pixbuf = value;
        }

        // Emulates Gtk.StatusIcon.TooltipText
        public string Tooltip
        {
            get => legacyIcon.TooltipText;
            set => legacyIcon.TooltipText = value;
        }

        // Emulates Gtk.StatusIcon.Visible
        public bool Visible
        {
            get => legacyIcon.Visible;
            set => legacyIcon.Visible = value;
        }

        public bool IsEmbedded
        {
            get => legacyIcon.IsEmbedded;
        }

        // Helper to set a named icon (as in IconName = "icon-name")
        public string IconName
        {
            get => legacyIcon.IconName;
            set => legacyIcon.IconName = value;
        }

        public void SetFromPixbuf(Pixbuf pixbuf)
        {
            legacyIcon.Pixbuf = pixbuf;
        }

        public void SetFromIconName(string iconName)
        {
            legacyIcon.IconName = iconName;
        }

        public void GetGeometry(out Screen screen, out Rectangle area, out Orientation orientation)
        {
            legacyIcon.GetGeometry(out screen, out area, out orientation);
        }

        public new void Dispose()
        {
            legacyIcon.Dispose();
        }
    }
}
