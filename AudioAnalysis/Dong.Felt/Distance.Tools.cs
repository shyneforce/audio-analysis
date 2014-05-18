﻿namespace Dong.Felt
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Drawing;
    using System.ComponentModel;
    using Representations;
    
    public enum Direction
    {
        /// <summary>
        /// This is North. Also known as vertical.
        /// </summary>
        [Description("This is East. Also known as horizontal.")]
        East = 0,

        /// <summary>
        /// This is North East. Also known as forward slash or diagonal.
        /// </summary>
        [Description("This is North East. Also known as forward slash or diagonal.")]
        NorthEast = 2,

        /// <summary>
        /// This is East. Also known as horizontal.
        /// </summary>
        [Description("This is North. Also known as vertital.")]
        North = 4,

        /// <summary>
        /// This is SouthEast. Also known as backward slash or diagonal.
        /// </summary>
        [Description("This is NorthWest. Also known as backward slash or diagonal.")]
        NorthWest = 6,
    }

    public static class Distance
    {
        
        /// <summary>
        /// To calculate the distance between two vectors by representing them with X and Y components. 
        /// </summary>
        /// <param name="ridgeNeighbourhood1"></param>
        /// <param name="ridgeNeighbourhood2"></param>
        /// <returns></returns>
        public static double VectorConversionDistance(RidgeDescriptionNeighbourhoodRepresentation ridgeNeighbourhood1, RidgeDescriptionNeighbourhoodRepresentation ridgeNeighbourhood2)
        {            
            var magnitude1 = ridgeNeighbourhood1.magnitude;
            var orientation1 = ridgeNeighbourhood1.orientation;
            var magnitude2 = ridgeNeighbourhood2.magnitude;
            var orientation2 = ridgeNeighbourhood2.orientation;
            var magnitudeX1 = magnitude1 * Math.Cos(orientation1);
            var magnitudeY1 = magnitude1 * Math.Sin(orientation1);
            var magnitudeX2 = magnitude2 * Math.Cos(orientation2);
            var magnitudeY2 = magnitude2 * Math.Sin(orientation2);
            var result = Math.Sqrt(Math.Pow((magnitudeX1 - magnitudeX2), 2) + Math.Pow((magnitudeY1 - magnitudeY2), 2));            
            return result; 
        }

        /// <summary>
        /// The euclidean distance.
        /// </summary>
        /// <param name="p1">
        /// The p 1.
        /// </param>
        /// <param name="p2">
        /// The p 2.
        /// </param>
        /// <returns>
        /// The distance between two points.
        /// </returns>
        public static double EuclideanDistanceForPoint(Point p1, Point p2)
        {
            var deltaX = Math.Pow(p2.X - p1.X, 2);
            var deltaY = Math.Pow(p2.Y - p1.Y, 2);

            return Math.Sqrt(deltaX + deltaY);
        }

        public static double EuclideanDistanceForCordinates(double x1, double y1, double x2, double y2)
        {
            var deltaX = Math.Pow(x2 - x1, 2);
            var deltaY = Math.Pow(y2 - y1, 2);

            return Math.Sqrt(deltaX + deltaY);
        }
        
        /// <summary>
        /// The manhanton distance.
        /// </summary>
        /// <param name="p1">
        /// The p 1.
        /// </param>
        /// <param name="p2">
        /// The p 2.
        /// </param>
        /// <returns>
        /// The <see cref="int"/>.
        /// </returns>
        public static int ManhattanDistance(Point p1, Point p2)
        {
            var deltaX = Math.Abs(p2.X - p1.X);
            var deltaY = Math.Abs(p2.Y - p1.Y);

            return deltaX + deltaY;
        }
    }
}
