// ---------------------------------------------------------------------------------
// Copyright (c) 2025 Maarten Jacobs
// All rights reserved.
// This file is part of tomboy-modern and was created specifically for this project.
// ---------------------------------------------------------------------------------
namespace Tomboy.Compat
{
    public class CompatUtils
    {
        public static Gdk.Color RgbaToColor(Gdk.RGBA rgba)
        {
            return new Gdk.Color(
                (byte)(rgba.Red * 255),
                (byte)(rgba.Green * 255),
                (byte)(rgba.Blue * 255)
            );
        }
    }
}