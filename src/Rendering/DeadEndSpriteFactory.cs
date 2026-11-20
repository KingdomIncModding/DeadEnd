using System;
using System.Collections.Generic;
using KingdomDeadEnd.Configuration;
using UnityEngine;

namespace KingdomDeadEnd.Rendering
{
    public static class DeadEndSpriteFactory
    {
        private const int NativePortSize = 22;
        private const int Padding = 7;
        private const int TextureSize = NativePortSize + Padding * 2;
        public const float OverlayUiSize = TextureSize * 2f;

        private static readonly Dictionary<(DeadEndIndicatorStyle, DeadEndOutlineThickness), Sprite> Cache =
            new Dictionary<(DeadEndIndicatorStyle, DeadEndOutlineThickness), Sprite>();

        private static readonly List<Texture2D> Textures = new List<Texture2D>();

        public static Sprite GetSprite(DeadEndIndicatorStyle style, DeadEndOutlineThickness thickness)
        {
            var effectiveThickness = style == DeadEndIndicatorStyle.MarkerOnly
                ? DeadEndOutlineThickness.Thin
                : thickness;

            var key = (style, effectiveThickness);
            if (Cache.TryGetValue(key, out Sprite existing) && existing)
                return existing;

            Sprite created = BuildSprite(style, effectiveThickness);
            Cache[key] = created;
            return created;
        }

        public static void Release()
        {
            foreach (Sprite sprite in Cache.Values)
            {
                if (sprite)
                    UnityEngine.Object.Destroy(sprite);
            }

            Cache.Clear();

            for (int i = 0; i < Textures.Count; i++)
            {
                if (Textures[i])
                    UnityEngine.Object.Destroy(Textures[i]);
            }

            Textures.Clear();
        }

        private static Sprite BuildSprite(DeadEndIndicatorStyle style, DeadEndOutlineThickness thickness)
        {
            var texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, mipChain: false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = $"DeadEnd_{style}_{thickness}"
            };

            var pixels = new Color32[TextureSize * TextureSize];

            if (style == DeadEndIndicatorStyle.Outline || style == DeadEndIndicatorStyle.OutlineAndMarker)
                PaintOutline(pixels, thickness);

            if (style == DeadEndIndicatorStyle.MarkerOnly || style == DeadEndIndicatorStyle.OutlineAndMarker)
                PaintMarker(pixels, style == DeadEndIndicatorStyle.OutlineAndMarker);

            texture.SetPixels32(pixels);
            texture.Apply(updateMipmaps: false, makeNoLongerReadable: true);
            Textures.Add(texture);

            return Sprite.Create(
                texture,
                new Rect(0f, 0f, TextureSize, TextureSize),
                new Vector2(0.5f, 0.5f),
                100f);
        }

        private static void PaintOutline(Color32[] pixels, DeadEndOutlineThickness thickness)
        {
            int maxDistSq = thickness == DeadEndOutlineThickness.Bold ? 5 : 2;
            var white = new Color32(255, 255, 255, 255);

            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    if (IsInsidePortSilhouette(x - Padding, y - Padding))
                        continue;

                    if (HasSilhouetteNeighbor(x - Padding, y - Padding, maxDistSq))
                        pixels[y * TextureSize + x] = white;
                }
            }
        }

        private static void PaintMarker(Color32[] pixels, bool offsetForOutline)
        {
            // Crisp 5x8 pixel-art hollow-center diamond beacon right at the open input port tip.
            int baseX = offsetForOutline ? 0 : 2;
            int baseY = 14;
            var white = new Color32(255, 255, 255, 255);

            string[] rows =
            {
                "..#..",
                ".###.",
                "#####",
                "##.##",
                "##.##",
                "#####",
                ".###.",
                "..#.."
            };

            for (int row = 0; row < rows.Length; row++)
            {
                string pattern = rows[row];
                for (int col = 0; col < pattern.Length; col++)
                {
                    if (pattern[col] != '#')
                        continue;

                    int px = baseX + col;
                    int py = baseY + row;
                    if (px < 0 || px >= TextureSize || py < 0 || py >= TextureSize)
                        continue;

                    if (IsInsidePortSilhouette(px - Padding, py - Padding))
                        continue;

                    pixels[py * TextureSize + px] = white;
                }
            }
        }

        private static bool HasSilhouetteNeighbor(int sx, int sy, int maxDistSq)
        {
            int radius = maxDistSq > 2 ? 2 : 1;
            for (int dy = -radius; dy <= radius; dy++)
            {
                for (int dx = -radius; dx <= radius; dx++)
                {
                    if (dx == 0 && dy == 0)
                        continue;

                    if (dx * dx + dy * dy > maxDistSq)
                        continue;

                    if (IsInsidePortSilhouette(sx + dx, sy + dy))
                        return true;
                }
            }

            return false;
        }

        private static bool IsInsidePortSilhouette(int sx, int sy)
        {
            if (sx < 0 || sx >= NativePortSize || sy < 0 || sy >= NativePortSize)
                return false;

            // Matches Kingdom Inc.'s exact 22x22 pentagonal port silhouette (IOGreen / IORed).
            int distanceFromMidline = sy <= 10 ? 10 - sy : sy - 11;
            int maxX = Math.Min(16, 21 - distanceFromMidline);
            return sx >= distanceFromMidline && sx <= maxX;
        }
    }
}
