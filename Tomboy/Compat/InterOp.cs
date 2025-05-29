// ---------------------------------------------------------------------------------
// Copyright (c) 2025 Maarten Jacobs
// All rights reserved.
// This file is part of tomboy-modern and was created specifically for this project.
// ---------------------------------------------------------------------------------
using System;
using System.Runtime.InteropServices;

namespace Tomboy.Compat
{
    internal static class GObjectInterop
    {
        [DllImport("libgobject-2.0.so.0")]
        public static extern IntPtr g_object_newv(IntPtr gType, uint nProperties, IntPtr pspecs, IntPtr values);

        [DllImport("libgobject-2.0.so.0")]
        public static extern IntPtr g_type_from_name(string name);

        [DllImport("libgobject-2.0.so.0")]
        public static extern void g_value_unset(ref GLib.Value val);

        [DllImport("libglib-2.0.so.0")]
        public static extern IntPtr g_strdup(string str);

        [DllImport("libglib-2.0.so.0")]
        public static extern void g_free(IntPtr ptr);

        [DllImport("libgobject-2.0.so.0")]
        public static extern ulong g_signal_connect_data(
                IntPtr instance,
                string detailed_signal,
                Delegate handler,
                IntPtr data,
                IntPtr destroy_data,
                GConnectFlags connect_flags
            );

        [Flags]
        public enum GConnectFlags
        {
            None = 0,
            After = 1,
            Swapped = 2
        }
    }
}