﻿// Copyright 2012 - Paul den Dulk (Geodan)

using BruTile.Cache;
using BruTile.Samples.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BruTile.Metro
{
    public static class TileLayer <T>
    {
        const long DurationOfAnimation = TimeSpan.TicksPerSecond;

        public static IEnumerable<Tile<T>> SelectTilesToRender(ITileCache<Tile<T>> cache, ITileSchema schema, Extent extent, double resolution)
        {
            var selection = new Dictionary<TileIndex, Tile<T>>();

            if (schema == null) return selection.Values;

            SelectRecursive(selection, cache, schema, extent, BruTile.Utilities.GetNearestLevel(schema.Resolutions, resolution));

            return SortOnLevel(selection, schema).Values;
        }

        private static Dictionary<TileIndex, Tile<T>> SortOnLevel(Dictionary<TileIndex, Tile<T>> selection,  ITileSchema schema)
        {
            return (from entry in selection orderby entry.Key.Level ascending select entry).ToDictionary(pair => pair.Key, pair => pair.Value);
        }

        public static void SelectRecursive(IDictionary<TileIndex, Tile<T>> selection, ITileCache<Tile<T>> cache, ITileSchema schema, Extent extent, int level)
        {
            if (level < 0) return;

            var tiles = schema.GetTilesInView(extent, level);

            foreach (TileInfo tileInfo in tiles)
            {
                var tile = cache.Find(tileInfo.Index);
                if (tile == null)
                {
                    SelectRecursive(selection, cache, schema, tileInfo.Extent.Intersect(extent), level - 1);
                }
                else
                {
                    selection[tileInfo.Index] = tile;
                    // If a tile is still semi transparent then select the higher level to show underneath the
                    // semi transparent one.
                    if (IsSemiTransparent(tile))
                    {
                        SelectRecursive(selection, cache, schema, tileInfo.Extent.Intersect(extent), level - 1);
                    }
                }
            }
        }

        public static bool IsSemiTransparent(Tile<T> tile)
        {
            if (tile.StartAnimation == default(long)) return false; // not yet shown at all
            var currentTime = DateTime.Now.Ticks;
            var timePassedSinceStartAnimation = currentTime - tile.StartAnimation;
            return timePassedSinceStartAnimation < DurationOfAnimation;
        }
    }
}
