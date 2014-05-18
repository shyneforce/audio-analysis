﻿using QutSensors.AudioAnalysis.AED;
using System;
using System.Collections.Generic;
using System.Text;

namespace CSharpTest
{
    class Program
    {
        static void Main(string[] args)
        {
            //Console.WriteLine(r + "");
            //Console.ReadKey();

            double[,] i = Util.fileToMatrix("Test\\Matlab\\I1.txt", 256, 5188);
            AcousticEventDetection.detectEvents(Default.intensityThreshold, Default.smallAreaThreshold, i);
        }
    }
}
