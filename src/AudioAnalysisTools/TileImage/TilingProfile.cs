// --------------------------------------------------------------------------------------------------------------------
// <copyright file="TilingProfile.cs" company="QutEcoacoustics">
// All code in this file and all associated files are the copyright and property of the QUT Ecoacoustics Research Group (formerly MQUTeR, and formerly QUT Bioacoustics Research Group).
// </copyright>
// <summary>
//   Defines the TileNamingPattern type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace AudioAnalysisTools.TileImage
{
    using System.Collections.Generic;
    using Acoustics.Shared;
    using SixLabors.ImageSharp;

    public abstract class TilingProfile
    {
        public virtual Color PaddingColor => Color.Transparent;

        public abstract int TileWidth { get; }

        public abstract int TileHeight { get; }

        public bool IsSquare => this.TileHeight == this.TileWidth;

        public virtual ImageChrome ChromeOption => ImageChrome.With;

        public virtual object GetZoomIndex(SortedSet<Layer> calculatedLayers, Layer selectedLayer)
        {
            return selectedLayer.ScaleIndex;
        }

        public virtual object GetTileIndexes(SortedSet<Layer> calculatedLayers, Layer selectedLayer, Point tileOffsets)
        {
            return tileOffsets;
        }

        public virtual string GetFileBaseName(SortedSet<Layer> calculatedLayers, Layer selectedLayer, Point tileOffsets)
        {
            var coordinates = (Point)this.GetTileIndexes(calculatedLayers, selectedLayer, tileOffsets);
            var zoomIndex = (double)this.GetZoomIndex(calculatedLayers, selectedLayer);
            return $"tile-{zoomIndex}_{coordinates.X}_{coordinates.Y}";
        }
    }
}