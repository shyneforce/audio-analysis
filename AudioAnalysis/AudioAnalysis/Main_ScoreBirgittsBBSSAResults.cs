﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using TowseyLib;
using System.Text.RegularExpressions;



namespace AudioAnalysis
{
    class Main_ScoreBirgittsBBSSAResults
    {
        public static bool DRAW_SONOGRAMS = false;

        public static void Main(string[] args)
        {
            Console.WriteLine("DATE AND TIME:" + DateTime.Now);
            Console.WriteLine("SCORING BIRGIT'S BBSD ALGORITHM\n");
            StringBuilder sb = new StringBuilder("DATE AND TIME:" + DateTime.Now + "\n");
            sb.Append("SCORING BIRGIT'S BBSD ALGORITHM\n");

            Log.Verbosity = 1;

            //#######################################################################################################
            // KEY PARAMETERS TO CHANGE for DETECT OSCILLATIONS
            //int minHz = 100;  //koalas range = 100-2000
            //int maxHz = 2000;
            //double dctDuration = 0.25;  //duration of DCT in seconds 
            //int dctIndex = 9;   //bounding index i.e. ignore oscillations with lower freq
            //double minAmplitude = 0.6;  //minimum acceptable value of a DCT coefficient
            //double scoreThreshold = 0.25; //USE THIS TO DETERMINE FP / FN trade-off.

            //BBSD results data
            string bbsdFolder = @"C:\SensorNetworks\TestResults\KoalaMale_BBSD\";
            string bbsdFile = "koalasBBSDAlgorithm_intThresh6_matchScore30.txt";
            string bbsdPath = bbsdFolder + bbsdFile;

            //LABELS FILE
            string labelsFileName = "Koala Calls - All 2009.txt";
            string labelsPath = @"C:\SensorNetworks\Recordings\KoalaMale\LargeTestSet\" + labelsFileName;

            //RESULTS FILE
            string resultsFile = "Koala Calls - BBSD.results.txt";
            string resultsPath = bbsdFolder + resultsFile;


            //#######################################################################################################

            //GET EVENTS from labels file
            if (!File.Exists(labelsPath))
            {
                Console.WriteLine("Cannot find file containing lebel data. <" + labelsPath + ">");
                Console.WriteLine("Press <ENTER> key to exit.");
                Console.ReadLine();
                System.Environment.Exit(999);
            }
            //Read list of acoustic events containg label data
            string labelsText;
            Log.WriteIfVerbose("Labels Path =" + labelsPath);
            List<AcousticEvent> labels = AcousticEvent.GetAcousticEventsFromLabelsFile(labelsPath, out labelsText);
            sb.Append("Labels Path =" + labelsPath + "\n");
            sb.Append(labelsText);


            //GET EVENTS from BBSD file
            if (!File.Exists(bbsdPath))
            {
                Console.WriteLine("Cannot find file containing bbsd data. <" + bbsdPath + ">");
                Console.WriteLine("Press <ENTER> key to exit.");
                Console.ReadLine();
                System.Environment.Exit(999);
            }
            //set up file containg BBSD data
            string bbsdText;
            Log.WriteIfVerbose("BBSD Path =" + bbsdPath);
            List<AcousticEvent> bbsdEvents = GetAcousticEventsFromBBSDFile(bbsdPath, out bbsdText);
            sb.Append("\nBBSD Path =" + bbsdPath + "\n");
            sb.Append(bbsdText);
            if(bbsdEvents.Count == 0)
            {
                Console.WriteLine("\nWARNING!!!!   BBSD file does not contain any Acoustic Events.");
                Console.WriteLine("Press <ENTER> key to exit.");
                Console.ReadLine();
                System.Environment.Exit(999);
            }



            int tp_total = 0;
            int fp_total = 0;
            int fn_total = 0;
            int label_count = 0;
  //          foreach (AcousticEvent ae in bbsdEvents)
   //         {
                label_count++;
                Log.WriteIfVerbose("\n\n" + label_count + " ###############################################################################################");
                sb.Append("\n\n" + label_count + " ###############################################################################################\n");

                //D: CALCULATE ACCURACY
                //Log.WriteIfVerbose("\n\n###############################################################################################");
                int tp, fp, fn;
                double precision, recall, accuracy;
                string resultsText;
                AcousticEvent.CalculateAccuracy(bbsdEvents, labels, out tp, out fp, out fn, out precision, out recall, out accuracy,
                                                                out resultsText);
                sb.Append(resultsText + "\n");
                sb.Append(String.Format("tp={0}\tfp={1}\tfn={2}\n", tp, fp, fn));
                Console.WriteLine("\ntp={0}\tfp={1}\tfn={2}", tp, fp, fn);
                sb.Append(String.Format("Recall={0:f2}  Precision={1:f2}  Accuracy={2:f2}\n", recall, precision, accuracy));
                Console.WriteLine("Recall={0:f2}  Precision={1:f2}  Accuracy={2:f2}\n", recall, precision, accuracy);

                tp_total += tp;
                fp_total += fp;
                fn_total += fn;

            //         }// end the foreach() loop 



            double precision_total, recall_total, accuracy_total;
            if (((tp_total + fp_total) == 0)) precision_total = 0.0;
            else precision_total = tp_total / (double)(tp_total + fp_total);
            if (((tp_total + fn_total) == 0)) recall_total = 0.0;
            else recall_total = tp_total / (double)(tp_total + fn_total);

            accuracy_total = (precision_total + recall_total) / (float)2;

            //write results to Console and to File
            Console.WriteLine("\n\n###############################################################################################");
            sb.Append("\n\n###############################################################################################\n");
            Console.WriteLine("\ntp={0}\tfp={1}\tfn={2}", tp_total, fp_total, fn_total);
            sb.Append(String.Format("\ntp={0}\tfp={1}\tfn={2}\n", tp_total, fp_total, fn_total));
            Console.WriteLine("Recall={0:f2}  Precision={1:f2}  Accuracy={2:f2}", recall_total, precision_total, accuracy_total);
            sb.Append(String.Format("Recall={0:f2}  Precision={1:f2}  Accuracy={2:f2}\n", recall_total, precision_total, accuracy_total));

            FileTools.WriteTextFile(bbsdFolder + resultsFile, sb.ToString());
            Console.WriteLine("\nFINISHED!");
            Console.ReadLine();
        }//end Main


        public static List<AcousticEvent> GetAcousticEventsFromBBSDFile(string path, out string bbsdText)
        {
            var sb = new StringBuilder();
            var events = new List<AcousticEvent>();
            List<string> lines = FileTools.ReadTextFile(path);
            int minFreq = 0; //dummy value - never to be used
            int maxfreq = 0; //dummy value - never to be used
            Console.WriteLine("\nList of BBSD events in file: " + Path.GetFileName(path));
            sb.Append("\nList of BBSD events in file: " + Path.GetFileName(path) + "\n");
            Console.WriteLine(" #  tag \tstart  ...   end  intensity quality  file");
            sb.Append(" #  tag \tstart  ...   end  intensity quality  file\n");
            for (int i = 1; i < lines.Count; i++) //skip the header line in labels data
            {
                string[] words = Regex.Split(lines[i], @"\t");
                if ((words.Length != 6) || (words[1].StartsWith("-")))
                    continue; //ignore entries that do not have full data
                string tag  = words[0];
                double start = Double.Parse(words[1]);
                double end = Double.Parse(words[2]);
                double score1 = Double.Parse(words[3]);
                double score2 = Double.Parse(words[4]);
                string file = words[5];
                if (file.EndsWith(".wav")) file = Path.GetFileNameWithoutExtension(file);//do not want file extention
                Console.WriteLine("{0}\t{1,10}{2,6:f1} ...{3,6:f1} {4,6:f1}  {5,10}", i, tag, start, end, score1, file);
                sb.Append(String.Format("{0}\t{1,10}{2,6:f1} ...{3,6:f1} {4,6:f1}  {5,10}\n", i, tag, start, end, score1, file));
                //Console.WriteLine(("").PadRight(24, '-'));

                var ae = new AcousticEvent(start, (end - start), minFreq, maxfreq);
                ae.Score = score1;
                ae.Name = tag;
                ae.SourceFile = file;
                //ae.Intensity = intensity;
                //ae.Quality = quality;
                events.Add(ae);
            }
            bbsdText = sb.ToString();
            return events;
        }
    
    
    }//end class

}
