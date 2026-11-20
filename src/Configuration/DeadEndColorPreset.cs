using System;
using UnityEngine;

namespace KingdomDeadEnd.Configuration
{
    public sealed class DeadEndColorPreset
    {
        public static readonly DeadEndColorPreset[] Presets =
        {
            new DeadEndColorPreset("Orange", "#FF8C1A", new Color(1.00f, 0.55f, 0.10f, 1f)),
            new DeadEndColorPreset("Deep Orange", "#FF6400", new Color(1.00f, 0.39f, 0.00f, 1f)),
            new DeadEndColorPreset("Amber", "#FFB326", new Color(1.00f, 0.70f, 0.15f, 1f)),
            new DeadEndColorPreset("Yellow", "#FFE033", new Color(1.00f, 0.88f, 0.20f, 1f)),
            new DeadEndColorPreset("Lime", "#7CFF38", new Color(0.49f, 1.00f, 0.22f, 1f)),
            new DeadEndColorPreset("Cyan", "#1CE8FF", new Color(0.11f, 0.91f, 1.00f, 1f)),
            new DeadEndColorPreset("Sky Blue", "#4DA6FF", new Color(0.30f, 0.65f, 1.00f, 1f)),
            new DeadEndColorPreset("Violet", "#B866FF", new Color(0.72f, 0.40f, 1.00f, 1f)),
            new DeadEndColorPreset("Magenta", "#FF42C8", new Color(1.00f, 0.26f, 0.78f, 1f)),
            new DeadEndColorPreset("Coral Red", "#FF4A4A", new Color(1.00f, 0.29f, 0.29f, 1f)),
            new DeadEndColorPreset("White", "#FFFFFF", Color.white)
        };

        public const string DefaultHex = "#FF8C1A";

        public string Name { get; }
        public string Hex { get; }
        public Color Color { get; }

        private DeadEndColorPreset(string name, string hex, Color color)
        {
            Name = name;
            Hex = hex;
            Color = color;
        }

        public static Color ResolveColor(string hex)
        {
            if (!string.IsNullOrWhiteSpace(hex) && ColorUtility.TryParseHtmlString(hex.Trim(), out Color parsed))
            {
                parsed.a = 1f;
                return parsed;
            }

            return Presets[0].Color;
        }

        public static string ToHex(Color color)
        {
            return "#" + ColorUtility.ToHtmlStringRGB(color);
        }
    }
}
