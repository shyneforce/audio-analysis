﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using TowseyLib;
using System.Text.RegularExpressions;

namespace AudioAnalysis
{
    public class AcousticEvent
    {

        //DIMENSIONS OF THE EVENT
        public double StartTime { get; set; } // (s),
        public double Duration; // in secondss
        public double EndTime { get; private set; } // (s),
        public int    MinFreq;  //
        public int    MaxFreq;  //
        public int    FreqRange { get { return(MaxFreq - MinFreq + 1); } }
        public bool   IsMelscale { get; set; } 
        public Oblong oblong { get; private set;}

        public int    FreqBinCount  { get; private set;}     //required for conversions to & from MEL scale
        public double FreqBinWidth  { get; private set; }    //required for freq-binID conversions
        public double FrameDuration { get; private set; }    //frame duration in seconds
        public double FrameOffset   { get; private set; }    //time between frame starts in seconds
        public double FramesPerSecond { get; private set; }  //inverse of the frame offset


        //PROPERTIES OF THE EVENTS i.e. Name, SCORE ETC
        public string Name  { get; set; }
        public string SourceFile { get; set; }
        public double Score { get; set; }
        public double NormalisedScore { get; private set; } //score normalised in range [0,1].
        //double I1MeandB; //mean intensity of pixels in the event prior to noise subtraction 
        //double I1Var;  //,
        //double I2MeandB; //mean intensity of pixels in the event after Wiener filter, prior to noise subtraction 
        //double I2Var;  //,
        double I3Mean;   //mean intensity of pixels in the event AFTER noise reduciton 
        double I3Var;    //variance of intensity of pixels in the event.

        public int Intensity { get; set; } //subjective assesment of event intenisty
        public int Quality { get; set; }   //subjective assessment of event quality
        public bool Tag { get; set; } //use this if want to filter or tag some members of a list for some purpose



        /// <summary>
        /// CONSTRUCTOR
        /// </summary>
        public AcousticEvent(double startTime, double duration, double minFreq, double maxFreq)
        {
            this.StartTime = startTime;
            this.Duration = duration;
            this.EndTime = startTime + duration;
            this.MinFreq = (int)minFreq;
            this.MaxFreq = (int)maxFreq;
            this.IsMelscale = false;
            oblong = null;// have no info to convert time/Hz values to coordinates
        }


        /// <summary>
        /// This constructor currently works ONLY for linear Herz scale events
        /// </summary>
        /// <param name="o"></param>
        /// <param name="binWidth"></param>
        public AcousticEvent(Oblong o, double frameOffset, double binWidth)
        {
            this.oblong       = o;
            this.FreqBinWidth = binWidth;
            this.FrameOffset  = frameOffset;
            this.IsMelscale   = false;

            double startTime; double duration;
            RowIDs2Time(o.r1, o.r2, frameOffset, out startTime, out duration);
            this.StartTime = startTime;
            this.Duration = duration;
            this.EndTime = startTime + duration;
            int minF; int maxF;
            HerzBinIDs2Freq(o.c1, o.c2, binWidth, out minF, out maxF);
            this.MinFreq = minF;
            this.MaxFreq = maxF;
        }

        public void DoMelScale(bool doMelscale, int freqBinCount)
        {
            this.IsMelscale   = doMelscale;
            this.FreqBinCount = freqBinCount;
        }

        public void SetTimeAndFreqScales(int samplingRate, int windowSize, int windowOffset)
        {
            double frameDuration, frameOffset, framesPerSecond;
            CalculateTimeScale(samplingRate, windowSize, windowOffset,
                                         out frameDuration, out frameOffset, out framesPerSecond);
            this.FrameDuration  = frameDuration;    //frame duration in seconds
            this.FrameOffset    = frameOffset;      //frame offset in seconds
            this.FramesPerSecond= framesPerSecond;  //inverse of the frame offset

            int binCount;
            double binWidth;
            CalculateFreqScale(samplingRate, windowSize, out binCount, out binWidth);
            this.FreqBinCount = binCount; //required for conversions to & from MEL scale
            this.FreqBinWidth = binWidth; //required for freq-binID conversions

            if (this.oblong == null) this.oblong = ConvertEvent2Oblong();

        }

        public void SetTimeAndFreqScales(double framesPerSec, double freqBinWidth)
        {
            //this.FrameDuration = frameDuration;     //frame duration in seconds
            this.FramesPerSecond = framesPerSec;      //inverse of the frame offset
            this.FrameOffset     = 1 / framesPerSec;  //frame offset in seconds

            //this.FreqBinCount = binCount;           //required for conversions to & from MEL scale
            this.FreqBinWidth = freqBinWidth;         //required for freq-binID conversions

            if (this.oblong == null) this.oblong = ConvertEvent2Oblong();
        }


        /// <summary>
        /// calculates the matrix/image indices of the acoustic event, when given the time/freq scales.
        /// This method called only by previous method:- SetTimeAndFreqScales(int samplingRate, int windowSize, int windowOffset)
        /// </summary>
        /// <returns></returns>
        public Oblong ConvertEvent2Oblong()
        {
            //translate time/freq dimensions to coordinates in a matrix.
            //columns of matrix are the freq bins. Origin is top left - as per matrix in the sonogram class.
            //Translate time dimension = frames = matrix rows.
            int topRow; int bottomRow;
            Time2RowIDs(this.StartTime, this.Duration, this.FrameOffset, out topRow, out bottomRow);

            //Translate freq dimension = freq bins = matrix columns.
            int leftCol; int rightCol;
            Freq2BinIDs(this.IsMelscale, this.MinFreq, this.MaxFreq, this.FreqBinCount, this.FreqBinWidth, out leftCol, out rightCol);

            return new Oblong(topRow, leftCol, bottomRow, rightCol);
        }

        /// <summary>
        /// Sets the passed score and also a value normalised between a min and a max.
        /// </summary>
        /// <param name="score"></param>
        /// <param name="min"></param>
        /// <param name="max"></param>
        public void SetScores(double score, double min, double max)
        {
            this.Score = score;
            this.NormalisedScore = (score - min) / (max - min);
            if (this.NormalisedScore > 1.0) this.NormalisedScore = 1.0;
            if (this.NormalisedScore < 0.0) this.NormalisedScore = 0.0;
        }

        public string WriteProperties()
        {
            return " min-max=" + MinFreq + "-" + MaxFreq + ",  " + oblong.c1 + "-" + oblong.c2;
        }

        /// <summary>
        /// Returns the first event in the passed list which overlaps with this one IN THE SAME RECORDING.
        /// If no event overlaps return null.
        /// </summary>
        /// <param name="events"></param>
        /// <returns></returns>
        public AcousticEvent OverlapsEventInList(List<AcousticEvent> events)
        {
            foreach (AcousticEvent ae in events)
            {
                if ((this.SourceFile.Equals(ae.SourceFile))&&(this.Overlaps(ae))) return ae;
            }
            return null;
        }

        /// <summary>
        /// Returns true/false if this event time-overlaps the passed event.
        /// Overlap in frequency dimension is ignored.
        /// The overlap determination is made on the start and end time points.
        /// There are two possible overlaps to be checked
        /// </summary>
        /// <param name="ae"></param>
        /// <returns></returns>
        public bool Overlaps(AcousticEvent ae)
        {
            if ((this.StartTime < ae.EndTime) && (this.EndTime > ae.StartTime)) 
                return true;
            if ((ae.StartTime < this.EndTime) && (ae.EndTime > this.StartTime)) 
                return true;
            return false;
        }

        //#################################################################################################################
        //METHODS TO CONVERT BETWEEN FREQ BIN AND HERZ OR MELS 

        /// <summary>
        /// converts frequency bounds of an event to left and right columns of object in sonogram matrix
        /// </summary>
        /// <param name="minF"></param>
        /// <param name="maxF"></param>
        /// <param name="leftCol"></param>
        /// <param name="rightCol"></param>
        public static void Freq2BinIDs(bool doMelscale, int minFreq, int maxFreq, int binCount, double binWidth,
                                                                                              out int leftCol, out int rightCol)
        {
            if(doMelscale)
                Freq2MelsBinIDs(minFreq, maxFreq, binWidth, binCount, out leftCol, out rightCol);
            else
                Freq2HerzBinIDs(minFreq, maxFreq, binWidth, out leftCol, out rightCol);
        }
        public static void Freq2HerzBinIDs(int minFreq, int maxFreq, double binWidth, out int leftCol, out int rightCol)
        {
            leftCol  = (int)Math.Round(minFreq / binWidth);
            rightCol = (int)Math.Round(maxFreq / binWidth);
        }
        public static void Freq2MelsBinIDs(int minFreq, int maxFreq, double binWidth, int binCount, out int leftCol, out int rightCol)
        {
                double nyquistFrequency = binCount * binWidth;
                double maxMel = Speech.Mel(nyquistFrequency); 
                int melRange = (int)(maxMel - 0 + 1);
                double binsPerMel = binCount / (double)melRange;
                leftCol  = (int)Math.Round((double)Speech.Mel(minFreq) * binsPerMel);
                rightCol = (int)Math.Round((double)Speech.Mel(maxFreq) * binsPerMel);
        }

        /// <summary>
        /// converts left and right column IDs to min and max frequency bounds of an event
        /// WARNING!!! ONLY WORKS FOR LINEAR HERZ SCALE. NEED TO WRITE ANOTHER METHOD FOR MEL SCALE ############################
        /// </summary>
        /// <param name="leftCol"></param>
        /// <param name="rightCol"></param>
        /// <param name="minFreq"></param>
        /// <param name="maxFreq"></param>
        public static void HerzBinIDs2Freq(int leftCol, int rightCol, double binWidth, out int minFreq, out int maxFreq)
        {
            minFreq = (int)Math.Round(leftCol * binWidth);
            maxFreq = (int)Math.Round(rightCol * binWidth);
            //if (doMelscale) //convert min max Hz to mel scale
            //{
            //}
        }


        
        
        //#################################################################################################################
        //METHODS TO CONVERT BETWEEN TIME BIN AND SECONDS 
        public static void RowIDs2Time(int topRow, int bottomRow, double frameOffset, out double startTime, out double duration)
        {
            startTime  = topRow * frameOffset;
            double end = (bottomRow + 1) * frameOffset;
            duration = end - startTime;
        }

        public static void Time2RowIDs(double startTime, double duration, double frameOffset, out int topRow, out int bottomRow)
        {
            topRow = (int)Math.Round(startTime / frameOffset);
            bottomRow = (int)Math.Round((startTime + duration) / frameOffset);
        }

        public void SetNetIntensityAfterNoiseReduction(double mean, double var)
        {
            this.I3Mean = mean; //
            this.I3Var  = var;  //
        }

        /// <summary>
        /// returns the frame duration and offset duration in seconds
        /// </summary>
        /// <param name="samplingRate"></param>
        /// <param name="windowSize"></param>
        /// <param name="windowOffset"></param>
        /// <param name="frameDuration">units = seconds</param>
        /// <param name="frameOffset">units = seconds</param>
        /// <param name="framesPerSecond"></param>
        public static void CalculateTimeScale(int samplingRate, int windowSize, int windowOffset,
                                                        out double frameDuration, out double frameOffset, out double framesPerSecond)
        {
            frameDuration = windowSize / (double)samplingRate;
            frameOffset = windowOffset / (double)samplingRate;
            framesPerSecond = 1 / frameOffset;
        }
        public static void CalculateFreqScale(int samplingRate, int windowSize, out int binCount, out double binWidth)
        {
            binCount = windowSize / 2;
            binWidth = samplingRate / (double)windowSize; //= Nyquist / binCount
        }


        /// <summary>
        /// returns all the events in a list that occur in the recording with passed file name.
        /// </summary>
        /// <param name="eventList"></param>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static List<AcousticEvent> GetEventsInFile(List<AcousticEvent> eventList, string fileName)
        {
            var events = new List<AcousticEvent>();
            foreach (AcousticEvent ae in eventList)
            {
                if(ae.SourceFile.Equals(fileName)) events.Add(ae);
            }
            return events;
        } // end method GetEventsInFile(List<AcousticEvent> eventList, string fileName)



        /// <summary>
        /// Reads a text file containing a list of acoustic events (one per line) and returns list of events.
        /// The file must contain a header.
        /// The format is tab separated words as follows:
        /// words[0]=file name; words[1]=recording date; words[2]=time; words[3]=start; words[4]=end; 
        /// words[5]=tag; words[6]=quality; words[7]=intensity 
        /// </summary>
        /// <param name="path">the file path</param>
        /// <returns>a list of Acoustic events</returns>
        public static List<AcousticEvent> GetAcousticEventsFromLabelsFile(string path, out string labelsText)
        {
            var sb = new StringBuilder();
            var events = new List<AcousticEvent>();
            List<string> lines = FileTools.ReadTextFile(path);
            int minFreq = 0; //dummy value - never to be used
            int maxfreq = 0; //dummy value - never to be used
            Console.WriteLine("\nList of labelled events in file: " + Path.GetFileName(path));
            sb.Append("\nList of labelled events in file: " + Path.GetFileName(path)+"\n");
            Console.WriteLine(" #  tag \tstart  ...   end  intensity quality  file");
            sb.Append(" #  tag \tstart  ...   end  intensity quality  file\n");
            for (int i = 1; i < lines.Count; i++) //skip the header line in labels data
            {
                string[] words = Regex.Split(lines[i], @"\t");
                if ((words.Length<8) || (words[4].Equals(null)) || (words[4].Equals(""))) 
                                              continue; //ignore entries that do not have full data
                string file = words[0];
                string date = words[1];
                string time = words[2];

                double start  = Double.Parse(words[3]);
                double end    = Double.Parse(words[4]);
                string tag    = words[5];
                int quality   = Int32.Parse(words[6]);
                int intensity = Int32.Parse(words[7]);
                Console.WriteLine(      "{0}\t{1,10}{2,6:f1} ...{3,6:f1}{4,10}{5,10}\t{6}", i, tag, start, end, intensity, quality, file);
                sb.Append(String.Format("{0}\t{1,10}{2,6:f1} ...{3,6:f1}{4,10}{5,10}\t{6}\n", i, tag, start, end, intensity, quality, file));
                //Console.WriteLine(("").PadRight(24, '-'));

                var ae = new AcousticEvent(start, (end - start), minFreq, maxfreq);
                ae.Score      = intensity;
                ae.Name       = tag;
                ae.SourceFile = file;
                ae.Intensity  = intensity;
                ae.Quality    = quality;
                events.Add(ae);
            }
            labelsText = sb.ToString();
            return events;
        } //end method GetLabelsInFile(List<string> labels, string file)





        public static void CalculateAccuracy(List<AcousticEvent> results, List<AcousticEvent> labels, out int tp, out int fp, out int fn,
                                         out double precision, out double recall, out double accuracy, out string resultsText)
        {
            //init  values
            tp = 0;
            fp = 0;
            fn = 0;
            //header
            string space = " ";
            int count = 0;
            List<string> sourceFiles = new List<string>();
            string header = String.Format("\nScore Category:    #{0,4}name{0,4}start{0,5}end{0,10}score{0,10}duration{0,1}", space);
            Console.WriteLine(header);
            string line = null;
            var sb = new StringBuilder(header + "\n"); 
            foreach (AcousticEvent ae in results)
            {
                count++;
                double end = ae.StartTime + ae.Duration;
                var events = AcousticEvent.GetEventsInFile(labels, ae.SourceFile);//get only events in same file as ae
                sourceFiles.Add(ae.SourceFile); //keep track of source files that the detected events come from
                AcousticEvent overlapEvent = ae.OverlapsEventInList(events);
                if (overlapEvent == null)
                {
                    fp++;
                    line = String.Format("False positive: {0,4} {1,10} {2,6:f1} ...{3,6:f1} {4,10:f2}\t{5,10:f2}\t{6}", count, ae.Name, ae.StartTime, end, ae.Score, ae.Duration, ae.SourceFile);
                }
                else
                {
                    tp++;
                    overlapEvent.Tag = true; //tag because later need to determine fn
                    line = String.Format("True  positive: {0,4} {1,10} {2,6:f1} ...{3,6:f1} {4,10:f2}\t{5,10:f2}\t{6}", count, ae.Name, ae.StartTime, end, ae.Score, ae.Duration, ae.SourceFile);
                }
                Console.WriteLine(line);
                sb.Append(line + "\n");

            }//end of looking for true and false positives

            //now calculate the fn. These are the labelled events not tagged in previous search.
            foreach (AcousticEvent ae in labels)
            {
                if (! sourceFiles.Contains(ae.SourceFile)) continue;
                if (ae.Tag == false)
                {
                    fn++;
                    line = String.Format("False NEGative:                {0:f1} ... {1:f1}  intensity={2} quality={3}",
                                              ae.StartTime, ae.EndTime, ae.Intensity, ae.Quality);
                    Console.WriteLine(line);
                    sb.Append(line + "\n");
                }
            }

            if (((tp + fp) == 0)) precision = 0.0;
            else precision = tp / (double)(tp + fp);
            if (((tp + fn) == 0)) recall = 0.0;
            else recall = tp / (double)(tp + fn);
            accuracy = (precision + recall) / (float)2;

            resultsText = sb.ToString();
        } //end method



    }
}
