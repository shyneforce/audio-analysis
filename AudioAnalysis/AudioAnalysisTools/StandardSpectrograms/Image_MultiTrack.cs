﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using TowseyLibrary;

namespace AudioAnalysisTools
{
    using Acoustics.Shared;

    public class Image_MultiTrack : IDisposable
    {

        #region Properties
        public Image sonogramImage { get; private set; }
        List<Image_Track> tracks = new List<Image_Track>();
        public IEnumerable<Image_Track> Tracks { get { return this.tracks; } }
        public IEnumerable<AcousticEvent> eventList { get; set; }
        public List<SpectralTrack> spectralTracks { get; set; }
        double[,] SuperimposedMatrix { get; set; }
        double[,] SuperimposedRedTransparency { get; set; }
        double[,] SuperimposedRainbowTransparency { get; set; }
        int[,] SuperimposedDiscreteColorMatrix { get; set; }
        private double superImposedMaxScore;
        private int[] FreqHits;
        private int nyquistFreq; //sets the frequency scale for drawing events
        private int freqBinCount;
        private double freqBinWidth;
        private double framesPerSecond;
        //private Point[] points;
        public List<PointOfInterest> Points { get; set; }
        #endregion


        /// <summary>
        /// CONSTRUCTOR
        /// </summary>
        /// <param name="image"></param>
        public Image_MultiTrack(Image image)
        {
            this.sonogramImage = image;
            this.Points = new List<PointOfInterest>();
        }

        public void AddTrack(Image_Track track)
        {
            this.tracks.Add(track);
        }

        public void AddEvents(IEnumerable<AcousticEvent> _list, int _nyquist, int _freqBinCount, double _framesPerSecond)
        {
            this.eventList       = _list;
            this.nyquistFreq     = _nyquist;
            this.freqBinCount    = _freqBinCount;
            this.framesPerSecond = _framesPerSecond;
            this.freqBinWidth    = _nyquist / _freqBinCount;
        }


        public void AddPoints(IEnumerable<PointOfInterest> points)
        {
            // this.points.AddRange(points);

            // scan for preexisting coordinates
            foreach (var pointOfInterest in points)
            {
                // copied to satisfy closure constraint
                PointOfInterest localCopy = pointOfInterest;
                
                // search current points to see if any share the same coordinates
                
                var match = this.Points.IndexOf(poi => poi.Point == localCopy.Point);                
                if (match >= 0)
                {
                    // if they do share the same coordinates, overwrite the old one
                    this.Points[match] = pointOfInterest;
                }
                else
                {
                    // otherwise, add new point to list
                    this.Points.Add(pointOfInterest);
                }
            }
        }

        public void OverlayRedMatrix(Double[,] m, double maxScore)
        {
            //this.SuperimposedMatrix = m; // TODO:  This line does not work !!?? Use next line
            this.SuperimposedRedTransparency = m;
            this.superImposedMaxScore = maxScore;
        }

        public void OverlayRedTransparency(Double[,] m)
        {
            this.SuperimposedRedTransparency = m;
        }

        public void OverlayRainbowTransparency(Double[,] m)
        {
            this.SuperimposedRainbowTransparency = m;
        }
        
        public void OverlayDiscreteColorMatrix(int[,] m)
        {
            this.SuperimposedDiscreteColorMatrix = m;
        }

        public void AddFreqHitValues(int[] f, int nyquist)
        {
            this.FreqHits = f;
            this.nyquistFreq = nyquist;
        }

        public void AddTracks(List<SpectralTrack> _tracks, double _framesPerSecond, double _freqBinWidth)
        {
            this.freqBinWidth = _freqBinWidth;
            this.framesPerSecond = _framesPerSecond;
            this.spectralTracks = _tracks;
        }

        /// <summary>
        /// WARNING: This method calls Image_MultiTrack.GetImage().
        /// In some circumstances GetImage() cannot manage images with an area larger than 10,385,000 pixels.
        /// This means it cannot handle recording sonograms longer than 2 minutes.
        /// </summary>
        /// <param name="path"></param>
        public void Save(string path)
        {
            Image image = this.GetImage();
            if (image == null)
            {
                Log.WriteLine("MultiTrackImage.Save() - WARNING: NULL IMAGE.Cannot save to: " + path);
                return;
            }
            image.Save(path, ImageFormat.Png);
            //image.Save(path, ImageFormat.Jpeg);
        }

        /// <summary>
        /// WARNING: graphics.DrawImage() or GDI cannot draw an image that is too big, typically 
        /// with an area larger than 10,385,000 pixels (Jiro estimated > 40000 pixels).
        /// This means it cannot handle recording sonograms longer than 2 minutes.
        /// Therefore call a recursive method to draw the image.
        /// </summary>
        /// <returns></returns>
        public Image GetImage()
        {
            // Calculate total height of the bmp
            var height = this.CalculateImageHeight();

            // set up a new image having the correct dimensions
            var imageToReturn = new Bitmap(this.sonogramImage.Width, height, PixelFormat.Format32bppArgb);
            

            // need to do this before get Graphics because cannot PutPixels into Graphics object.
            if (this.SuperimposedRedTransparency != null)
            {
                this.sonogramImage = this.OverlayRedTransparency((Bitmap)this.sonogramImage);
            }
            if (this.SuperimposedMatrix != null)
            {
                this.sonogramImage = this.OverlayMatrix((Bitmap)this.sonogramImage);
            }

            // create new graphics canvas and add in the sonogram image
            using (var g = Graphics.FromImage(imageToReturn))
            {
                ////g.DrawImage(this.SonoImage, 0, 0);          // WARNING ### THIS CALL DID NOT WORK THEREFORE
                GraphicsSegmented.Draw(g, this.sonogramImage);  // USE THIS CALL INSTEAD.

                // draw events first because their rectangles can cover other features
                if (this.eventList != null)
                {
                    var hitImage = new Bitmap(imageToReturn.Width, height, PixelFormat.Format32bppArgb);
                    //hitImage.MakeTransparent();
                    foreach (AcousticEvent e in this.eventList)
                    {
                        e.DrawEvent(g, hitImage, this.framesPerSecond, this.freqBinWidth, this.sonogramImage.Height);
                    }

                    g.DrawImage(hitImage, 0, 0);
                }

                // draw events first because their rectangles can cover other features
                if (this.Points != null) 
                {
                    // var stats = new StatDescriptive(this.points.Select(p => p.Item2).ToArray());
                    // stats.Analyze(); 
                    foreach (PointOfInterest poi in this.Points)
                    {
                        //poi.DrawPoint(g, this.Points, this.sonogramImage.Height);
                        const int Radius = 12;
                        poi.DrawBox(g, this.Points, Radius);
                    }       
                }

                // draw spectral tracks
                if (this.spectralTracks != null)  
                {
                    foreach (SpectralTrack t in this.spectralTracks)
                    {
                        t.DrawTrack(g, this.framesPerSecond, this.freqBinWidth, this.sonogramImage.Height);
                    }
                }

                if (this.FreqHits != null)
                {
                    this.DrawFreqHits(g);
                }

                if (this.SuperimposedRainbowTransparency != null)
                {
                    this.OverlayRainbowTransparency(g, (Bitmap)this.sonogramImage);
                }

                if (this.SuperimposedDiscreteColorMatrix != null)
                {
                    this.OverlayDiscreteColorMatrix(g, (Bitmap)this.sonogramImage);
                }
            }

            // now add tracks to the image
            int offset = this.sonogramImage.Height;
            foreach (Image_Track track in this.tracks)
            {
                track.topOffset = offset;
                track.bottomOffset = offset + track.Height - 1;
                track.DrawTrack(imageToReturn);
                offset += track.Height;
            }

            return imageToReturn;
        }

        private int CalculateImageHeight()
        {
            int totalHeight = this.sonogramImage.Height;
            foreach (Image_Track track in this.tracks)
                totalHeight += track.Height;
            return totalHeight;
        }

        void DrawFreqHits(Graphics g)
        {
            int L = this.FreqHits.Length;
            Pen p1 = new Pen(Color.Red);
            //Pen p2 = new Pen(Color.Black);
            for (int x = 0; x < L; x++)
            {
                if (this.FreqHits[x] <= 0) continue;
                int y = (int)(this.sonogramImage.Height * (1 - (this.FreqHits[x] / (double)this.nyquistFreq)));
                //g.DrawRectangle(p1, x, y, x + 1, y + 1);
                g.DrawLine(p1, x, y, x, y + 1);
                //g.DrawString(e.Name, new Font("Tahoma", 6), Brushes.Black, new PointF(x, y - 1));
            }
        }

        /// <summary>
        /// superimposes a matrix of scores on top of a sonogram.
        /// Only draws lines on every second row so that the underling sonogram can be discerned
        /// </summary>
        /// <param name="g"></param>
        void OverlayMatrix(Graphics g)
        {
            //int paletteSize = 256;
            var pens = ImageTools.GetRedGradientPalette(); //size = 256

            int rows = this.SuperimposedMatrix.GetLength(0);
            int cols = this.SuperimposedMatrix.GetLength(1);
            int imageHt = this.sonogramImage.Height - 1; //subtract 1 because indices start at zero
            //ImageTools.DrawMatrix(DataTools.MatrixRotate90Anticlockwise(this.SuperimposedMatrix), @"C:\SensorNetworks\WavFiles\SpeciesRichness\Dev1\superimposed1.png", false);

            for (int c = 1; c < cols; c++)//traverse columns - skip DC column
            {
                for (int r = 0; r < rows; r++)
                {
                    if (this.SuperimposedMatrix[r, c] == 0.0) continue;
                    double normScore = this.SuperimposedMatrix[r, c] / (double)this.superImposedMaxScore;
                    //int penID = (int)(paletteSize * normScore);
                    //if (penID >= paletteSize) penID = paletteSize - 1;
                    var brush = new SolidBrush(Color.Red);
                    g.FillRectangle(brush, r, imageHt - c, 1, 1); //THIS DRAWS A PIXEL !!!!
                }
                //c++; //only draw on every second row.
            }
        } //OverlayMatrix()

        /// superimposes a matrix of scores on top of a sonogram.
        /// Only draws lines on every second row so that the underling sonogram can be discerned
        /// </summary>
        /// <param name="g"></param>
        public Bitmap OverlayMatrix(Bitmap bmp)
        {
            Bitmap newBmp = (Bitmap)bmp.Clone();
            //int paletteSize = 256;
            var pens = ImageTools.GetRedGradientPalette(); //size = 256

            int rows = this.SuperimposedMatrix.GetLength(0);
            int cols = this.SuperimposedMatrix.GetLength(1);
            int imageHt = this.sonogramImage.Height - 1; //subtract 1 because indices start at zero

            for (int c = 1; c < cols; c++)//traverse columns - skip DC column
            {
                for (int r = 0; r < rows; r++)
                {
                    if (this.SuperimposedMatrix[r, c] == 0.0) continue;
                    double normScore = this.SuperimposedMatrix[r, c] / (double)this.superImposedMaxScore;
                    //int penID = (int)(paletteSize * normScore);
                    //if (penID >= paletteSize) penID = paletteSize - 1;
                    //g.DrawLine(pens[penID], r, imageHt - c, r + 1, imageHt - c);
                    //g.DrawLine(new Pen(Color.Red), r, imageHt - c, r + 1, imageHt - c);
                    newBmp.SetPixel(r, imageHt - c, Color.Red);
                }
            }
            return newBmp;
        } //OverlayMatrix()



        /// <summary>
        /// superimposes a matrix of scores on top of a sonogram.
        /// </summary>
        /// <param name="g"></param>
        public Bitmap OverlayRedTransparency(Bitmap bmp)
        {
            Bitmap newBmp = (Bitmap)bmp.Clone();
            int rows = this.SuperimposedRedTransparency.GetLength(0);
            int cols = this.SuperimposedRedTransparency.GetLength(1);
            int imageHt = this.sonogramImage.Height - 1; //subtract 1 because indices start at zero

            for (int c = 1; c < cols; c++)//traverse columns - skip DC column
            {
                for (int r = 0; r < rows; r++)
                {
                    if (this.SuperimposedRedTransparency[r, c] == 0.0) continue;
                    Color pixel = bmp.GetPixel(r, imageHt - c);
                    if (pixel.R == 255) continue; //white
                    newBmp.SetPixel(r, imageHt - c, Color.FromArgb(255, pixel.G, pixel.B));
                }
            }
            return newBmp;
        } //OverlayRedTransparency()

        /// <summary>
        /// superimposes a matrix of scores on top of a sonogram. USES RAINBOW PALLETTE
        /// ASSUME MATRIX NORMALIZED IN [0,1]
        /// </summary>
        /// <param name="g"></param>
        void OverlayRainbowTransparency(Graphics g, Bitmap bmp)
        {
            Color[] palette = { Color.Crimson, Color.Red, Color.Orange, Color.Yellow, Color.Lime, Color.Green, Color.Blue, Color.Indigo, Color.Violet, Color.Purple };
            int rows = this.SuperimposedRainbowTransparency.GetLength(0);
            int cols = this.SuperimposedRainbowTransparency.GetLength(1);
            int imageHt = this.sonogramImage.Height - 1; //subtract 1 because indices start at zero

            for (int r = 0; r < rows; r++)
            {
                for (int c = 1; c < cols; c++)//traverse columns - skip DC column
                {
                    double value = this.SuperimposedRainbowTransparency[r, c];
                    if (value <= 0.0) continue; //nothing to show
                    Color pixel = bmp.GetPixel(r, imageHt - c);
                    if (pixel.R > 250) continue; //by-pass white
                    int index = (int)Math.Floor((value * 9));//get index into pallette
                    if (index > 9) index = 9;
                    Color newColor = palette[index];
                    double factor = pixel.R / (double)(255 * 1.2);  //1.2 is a color intensity adjustment
                    int red = (int)Math.Floor(newColor.R + ((255 - newColor.R) * factor));
                    int grn = (int)Math.Floor(newColor.G + ((255 - newColor.G) * factor));
                    int blu = (int)Math.Floor(newColor.B + ((255 - newColor.B) * factor));
                    g.DrawLine(new Pen(Color.FromArgb(red, grn, blu)), r, imageHt - c, r + 1, imageHt - c);
                    c++; //every second column
                }
            }
        } //OverlayRainbowTransparency()


        /// <summary>
        /// superimposes a matrix of scores on top of a sonogram. USES RAINBOW PALLETTE
        /// ASSUME MATRIX consists of integers >=0; 
        /// </summary>
        /// <param name="g"></param>
        void OverlayDiscreteColorMatrix(Graphics g, Bitmap bmp)
        {
            int rows = this.SuperimposedDiscreteColorMatrix.GetLength(0);
            int cols = this.SuperimposedDiscreteColorMatrix.GetLength(1);
            int min, max;
            MatrixTools.MinMax(this.SuperimposedDiscreteColorMatrix, out min, out max);

            int length = max - min + 1;

            int palleteLength = ImageTools.darkColors.Length;
            //Color[] palette = { Color.Crimson, Color.Red, Color.Orange, Color.Yellow, Color.Lime, Color.Green, Color.Blue, Color.Indigo, Color.Violet, Color.Purple };
            int imageHt = this.sonogramImage.Height - 1; //subtract 1 because indices start at zero

            for (int r = 0; r < rows; r++)
            {
                for (int c = 1; c < cols; c++)//traverse columns - skip DC column
                {
                    int index = this.SuperimposedDiscreteColorMatrix[r, c];
                    if (index <= 0) continue; //nothing to show
                    //Color pixel = bmp.GetPixel(r, imageHt - c);
                    //if (pixel.R > 250) continue; //by-pass white
                    //int index = (int)Math.Floor((value * 9));//get index into pallette
                    if (index >= palleteLength) index = index % palleteLength;
                    Color newColor = ImageTools.darkColors[index];
                    //double factor = pixel.R / (double)(255 * 1.2);  //1.2 is a color intensity adjustment
                    //int red = (int)Math.Floor(newColor.R + ((255 - newColor.R) * factor));
                    //int grn = (int)Math.Floor(newColor.G + ((255 - newColor.G) * factor));
                    //int blu = (int)Math.Floor(newColor.B + ((255 - newColor.B) * factor));
                    //g.DrawLine(new Pen(Color.FromArgb(red, grn, blu)), r, imageHt - c, r + 1, imageHt - c);
                    g.DrawLine(new Pen(newColor), r, imageHt - c, r + 1, imageHt - c);
                }
            }
        } //OverlayDiscreteColorMatrix()
        


        #region IDisposable Members

        public void Dispose()
        {
            this.eventList = null;
            this.sonogramImage.Dispose();
        }

        #endregion
    } //end class
}