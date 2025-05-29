// ---------------------------------------------------------------------------------
// Copyright (c) 2025 Maarten Jacobs
// All rights reserved.
// This file is part of tomboy-modern and was created specifically for this project.
// ---------------------------------------------------------------------------------
using System;
using System.Runtime.InteropServices;
using Gtk;

namespace Tomboy.Compat
{
    public class CompatDialog : Dialog
    {
        private readonly Box container;
        public Box VBox => container;

        public CompatDialog(Window parent, DialogFlags flags, string title = null, params object[] button_data)
            : base(CreateObjectWithProperties(
                        "GtkDialog",
                        [
                            "use-header-bar", false,
                            "modal", flags.HasFlag(DialogFlags.Modal),
                            "destroy-with-parent", flags.HasFlag(DialogFlags.DestroyWithParent)
                        ]
                ))
        {
            Title = title ?? "";
            TransientFor = parent;
            Modal = true;
            BorderWidth = 10;
            DefaultWidth = 400;

            container = ContentArea;
            
            for (int i = 0; i < button_data.Length - 1; i += 2)
            {
                AddButton((string)button_data[i], (int)button_data[i + 1]);
            }
        }

        private static IntPtr CreateObjectWithProperties(string gtypeName, object[] properties)
        {
            if (properties.Length % 2 != 0)
                throw new ArgumentException("Properties must be in name/value pairs");

            int propCount = properties.Length / 2;
            IntPtr[] pspecNames = new IntPtr[propCount];
            GLib.Value[] gvalues = new GLib.Value[propCount];

            try
            {
                for (int i = 0; i < propCount; ++i)
                {
                    if (properties[i * 2] is not string name)
                        throw new ArgumentException($"Expected string for property name at index {i * 2}");

                    pspecNames[i] = GObjectInterop.g_strdup(name);
                    gvalues[i] = new GLib.Value(properties[i * 2 + 1]);
                }

                // IntPtr gtype = GObjectInterop.g_type_from_name(gtypeName);
                GLib.GType gtypeStruct = GLib.GType.FromName("GtkDialog");
                IntPtr gtype = gtypeStruct.Val;
                if (gtype == IntPtr.Zero)
                    throw new ArgumentException($"Unknown GType name: {gtypeName}");

                IntPtr pspecsPtr = Marshal.UnsafeAddrOfPinnedArrayElement(pspecNames, 0);
                IntPtr valuesPtr = Marshal.UnsafeAddrOfPinnedArrayElement(gvalues, 0);

                return GObjectInterop.g_object_newv(gtype, (uint)propCount, pspecsPtr, valuesPtr);
            }
            finally
            {
                for (int i = 0; i < propCount; ++i)
                {
                    GObjectInterop.g_value_unset(ref gvalues[i]);
                    GObjectInterop.g_free(pspecNames[i]);
                }
            }
        }
    }
}