﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Audio2InputForConvCNN.cs" company="QutBioacoustics">
//   All code in this file and all associated files are the copyright of the QUT Bioacoustics Research Group (formally MQUTeR).
// </copyright>
// <summary>
//   Defines the Audio2InputForConvCNN type.
//   ACTIVITY CODE: audio2InputForConvCNN
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace AnalysisPrograms
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Reflection;

    using Acoustics.Shared;
    using Acoustics.Shared.Csv;
    using Acoustics.Tools;
    using Acoustics.Tools.Audio;

    using AnalysisBase;
    using AnalysisBase.ResultBases;

    using AnalysisPrograms.Production;

    using AudioAnalysisTools;
    using AudioAnalysisTools.DSP;
    using AudioAnalysisTools.LongDurationSpectrograms;
    using AudioAnalysisTools.StandardSpectrograms;
    using AudioAnalysisTools.WavTools;

    using log4net;

    using PowerArgs;

    using TowseyLibrary;

    public class Audio2InputForConvCNN
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        // use the following paths for the command line for the <audio2sonogram> task. 
        // audio2InputForConvCNN "Path to CSV file"   @"C:\Work\GitHub\audio-analysis\AudioAnalysis\AnalysisConfigFiles\Mangalam.Sonogram.yml"  "Output directory" true
        [CustomDetailedDescription]
        [CustomDescription]
        public class Arguments : SourceConfigOutputDirArguments
        {
            public bool Verbose { get; set; }

            public static string Description()
            {
                return "Generates multiple spectrogram images and SNR info.";
            }

            public static string AdditionalNotes()
            {
                return "The Source file in this case is a csv file showing locations of short audio segments and the call bounds within each audio segment.";
            }
        }



        private static Arguments Dev()
        {

            return new Arguments
            {

            // prior to processing
                //Y:\Results\2014Aug29-000000 - ConvDNN Data Export\ConvDNN_annotation_export_commonNameOnly_withPadding_20140829.csv
            //audio_event_id	audio_recording_id	audio_recording_uuid	event_created_at_utc	projects	site_id	site_name	event_start_date_utc	event_start_seconds	event_end_seconds	event_duration_seconds	low_frequency_hertz	high_frequency_hertz	padding_start_time_seconds	padding_end_time_seconds	common_tags	species_tags	other_tags	listen_url	library_url

                // Y:\Results\2014Aug29-000000 - ConvDNN Data Export\Output\ConvDNN_annotation_export_commonNameOnly_withPadding_20140829.processed.csv
            //audio_event_id	audio_recording_id	audio_recording_uuid	event_created_at_utc	projects	site_id	site_name	event_start_date_utc	event_start_seconds	event_end_seconds	event_duration_seconds	low_frequency_hertz	high_frequency_hertz	padding_start_time_seconds	padding_end_time_seconds	common_tags	species_tags	other_tags	listen_url	library_url	path	download_success	skipped

                // csv file containing recording info, call bounds etc
                //Source = @"Y:\Results\2014Aug29-000000 - ConvDNN Data Export\Output\mangalam_annotation_export_commonNameOnly_withPadding_20140829.processed.csv".ToFileInfo(),
                //Source = @"C:\SensorNetworks\Output\ConvDNN\ConvDNN_annotation_export_commonNameOnly_withPadding_20140829.processed.csv".ToFileInfo(),
                Source = @"C:\SensorNetworks\WavFiles\ConvDNNData\ConvDNN_annotation_export_commonNameOnly_withPadding_20140829.processed.csv".ToFileInfo(),

                Config = @"C:\Work\GitHub\audio-analysis\AudioAnalysis\AnalysisConfigFiles\Mangalam.Sonogram.yml".ToFileInfo(),

                Output = @"C:\SensorNetworks\Output\ConvDNN".ToDirectoryInfo(),
                Verbose = true
            };

            throw new NoDeveloperMethodException();
        }



        /// <summary>
        /// This method written 18-09-2014 to process Mangalam's CNN recordings.
        /// Calculate the SNR statistics for each recording and then write info back to csv file
        /// </summary>
        //public static void PreprocessConvCNNData()
        //{
        //    FileInfo configFile = @"C:\Work\GitHub\audio-analysis\AudioAnalysis\AnalysisConfigFiles\Mangalam.Sonogram.yml".ToFileInfo();

        //    // prior to processing
        //    //Y:\Results\2014Aug29-000000 - Mangalam Data Export\mangalam_annotation_export_commonNameOnly_withPadding_20140829.csv
        //    //audio_event_id	audio_recording_id	audio_recording_uuid	event_created_at_utc	projects	site_id	site_name	event_start_date_utc	event_start_seconds	event_end_seconds	event_duration_seconds	low_frequency_hertz	high_frequency_hertz	padding_start_time_seconds	padding_end_time_seconds	common_tags	species_tags	other_tags	listen_url	library_url

        //    // Y:\Results\2014Aug29-000000 - Mangalam Data Export\Output\mangalam_annotation_export_commonNameOnly_withPadding_20140829.processed.csv
        //    //audio_event_id	audio_recording_id	audio_recording_uuid	event_created_at_utc	projects	site_id	site_name	event_start_date_utc	event_start_seconds	event_end_seconds	event_duration_seconds	low_frequency_hertz	high_frequency_hertz	padding_start_time_seconds	padding_end_time_seconds	common_tags	species_tags	other_tags	listen_url	library_url	path	download_success	skipped

        //    // csv file containing recording info, call bounds etc
        //    FileInfo csvFileInfo = @"Y:\Results\2014Aug29-000000 - Mangalam Data Export\Output\mangalam_annotation_export_commonNameOnly_withPadding_20140829.processed.csv".ToFileInfo();

        //}




        public static void Main(Arguments arguments)
        {
            if (arguments == null)
            {
                arguments = Dev();
            }

            if (!arguments.Output.Exists) arguments.Output.Create();

            const string Title = "# PRE-PROCESS SHORT AUDIO RECORDINGS FOR Convolutional DNN";
            string date = "# DATE AND TIME: " + DateTime.Now;
            LoggedConsole.WriteLine(Title);
            LoggedConsole.WriteLine(date);
            LoggedConsole.WriteLine("# Input csv file: " + arguments.Source.Name);
            LoggedConsole.WriteLine("# Config    file: " + arguments.Config.Name);
            LoggedConsole.WriteLine("# Output dirctry: " + arguments.Output.Name);


            bool verbose = arguments.Verbose;

            // 1. set up the necessary files
            FileInfo csvFileInfo = arguments.Source;
            FileInfo configFile = arguments.Config;
            DirectoryInfo opDir = arguments.Output;

            // 2. get the config dictionary
            dynamic configuration = Yaml.Deserialise(configFile);

            // below four lines are examples of retrieving info from dynamic config
            //dynamic configuration = Yaml.Deserialise(configFile);
            // string analysisIdentifier = configuration[AnalysisKeys.AnalysisName];
            // int resampleRate = (int?)configuration[AnalysisKeys.ResampleRate] ?? AppConfigHelper.DefaultTargetSampleRate;


            var configDict = new Dictionary<string, string>((Dictionary<string, string>)configuration);


            configDict[AnalysisKeys.AddAxes] = ((bool?)configuration[AnalysisKeys.AddAxes] ?? true).ToString();
            configDict[AnalysisKeys.AddSegmentationTrack] = configuration[AnalysisKeys.AddSegmentationTrack] ?? true;

            //bool makeSoxSonogram = (bool?)configuration[AnalysisKeys.MakeSoxSonogram] ?? false;
            configDict[AnalysisKeys.AddTimeScale] = (string)configuration[AnalysisKeys.AddTimeScale] ?? "true";
            configDict[AnalysisKeys.AddAxes] = (string)configuration[AnalysisKeys.AddAxes] ?? "true";
            configDict[AnalysisKeys.AddSegmentationTrack] = (string)configuration[AnalysisKeys.AddSegmentationTrack] ?? "true";

            //IMPORTANT PARAMETER - SET EQUAL TO WHAT ANTHONY HAS EXTRACTED.
            double extractTimeDuration = 4.0; // fixed length duration of all extracts from the original data - centred on the bounding box.

            // print out the parameters
            if (verbose)
            {
                LoggedConsole.WriteLine("\nPARAMETERS");
                foreach (KeyValuePair<string, string> kvp in configDict)
                {
                    LoggedConsole.WriteLine("{0}  =  {1}", kvp.Key, kvp.Value);
                }
            }


            //set up the output file
            //string header = "audio_event_id,audio_recording_id,audio_recording_uuid,projects,site_name,event_start_date_utc,event_duration_seconds,common_tags,species_tags,other_tags,path,Threshold,Snr,FractionOfFramesGTThreshold,FractionOfFramesGTHalfSNR";
            string header = "audio_event_id,site_name,common_tags,Threshold,Snr,FractionOfFramesGTThreshold,FractionOfFramesGTThirdSNR,path2Spectrograsms";
            string opPath = Path.Combine(opDir.FullName, "SNRDataForConvDNN_DataSet_23thSept2014.csv");
            using (StreamWriter writer = new StreamWriter(opPath))
            {
                writer.WriteLine(header);
            }

            // keep counter on file availability
            int lineNumber = 0;
            int fileExistsCount = 0;
            int fileLocationNotInCsv = 0;
            int fileInCsvDoesNotExist = 0;

            // read through the csv file containing info about recording locations and call bounds
            string strLine;
            try
            {
                FileStream aFile = new FileStream(csvFileInfo.FullName, FileMode.Open);
                StreamReader sr = new StreamReader(aFile);
                // read the header and discard
                strLine = sr.ReadLine();
                lineNumber++;

                while ((strLine = sr.ReadLine()) != null)
                {
                    lineNumber++;
                    if (lineNumber % 1000 == 0) Console.WriteLine(lineNumber);

                    // cannot use next line because reads the entire file
                    //var data = Csv.ReadFromCsv<string[]>(csvFileInfo).ToList();
                    // read single record from csv file
                    var record = CsvDataRecord.ReadLine(strLine);

                    if (record.path == null)
                    {
                        fileLocationNotInCsv++;
                        //string warning = String.Format("######### WARNING: line {0}  NULL PATH FIELD >>>null<<<", count);
                        //LoggedConsole.WriteWarnLine(warning);
                        continue;
                    }

                    FileInfo sourceRecording = record.path;
                    string fileName = sourceRecording.Name;
                    DirectoryInfo sourceDirectory = sourceRecording.Directory;
                    string directoryName = sourceDirectory.Name;
                    string parentDirectoryName = sourceDirectory.Parent.Name;
                    DirectoryInfo imageOpDir = new DirectoryInfo(opDir.FullName + @"\" + parentDirectoryName);
                    //DirectoryInfo imageOpDir = new DirectoryInfo(opDir.FullName + @"\" + parentDirectoryName + @"\" + directoryName);

                    //#######################################
                    //#######################################
                    // my debug code for home to test on subset of data - comment these lines when doing the real thing! 
                    //#######################################
                    DirectoryInfo localSourceDir = new DirectoryInfo(@"C:\SensorNetworks\WavFiles\ConvDNNData");
                    sourceRecording = Path.Combine(localSourceDir.FullName + @"\" + parentDirectoryName + @"\" + directoryName, fileName).ToFileInfo();
                    //#######################################
                    //#######################################


                    if (! sourceRecording.Exists)
                    {
                        fileInCsvDoesNotExist ++;
                        string warning = String.Format("FILE DOES NOT EXIST >>>," + sourceRecording.Name);
                        using (StreamWriter writer = new StreamWriter(opPath, true))
                        {
                            writer.WriteLine(warning);
                        }
                        //LoggedConsole.WriteWarnLine(warning);
                        continue;
                    }

                    // everything should be OK - have jumped through all the hoops.
                    fileExistsCount ++;
                    // string message = String.Format("#########: line {0}  FILE EXISTS >>>" + sourceRecording.Name + "<<<", fileExistsCount);
                    // LoggedConsole.WriteWarnLine(message);
                    DirectoryInfo sourceDir = sourceRecording.Directory;
                    int minHz = record.low_frequency_hertz;
                    int maxHz = record.high_frequency_hertz;
                    TimeSpan start = record.event_start_seconds;
                    TimeSpan duration = record.event_duration_seconds;
                    TimeSpan localStart = TimeSpan.Zero;
                    TimeSpan extractDuration = TimeSpan.FromSeconds(extractTimeDuration);
                    //TimeSpan paddingStart = record.event_start_seconds;
                    //continue;

                    // ####################################################################

                    var result = AnalyseOneRecording(sourceRecording, configDict, localStart, extractDuration, minHz, maxHz, imageOpDir);

                    // CONSTRUCT the outputline for csv file
                    // "audio_event_id,audio_recording_id,audio_recording_uuid,projects,site_name,event_start_date_utc,event_duration_seconds,common_tags,species_tags,other_tags,path,Threshold,Snr,FractionOfFramesGTThreshold,FractionOfFramesGTHalfSNR";
                    //  audio_event_id,site_name,common_tags,Threshold,Snr,FractionOfFramesGTThreshold,FractionOfFramesGTThirdSNR,path
                    string line = String.Format("{0},{1},{2},{3:f2},{4:f3},{5:f3},{6:f3},{7}",
                                                record.audio_event_id, record.site_name, record.common_tags, result.SnrStatistics.Threshold, result.SnrStatistics.Snr,
                                                result.SnrStatistics.FractionOfFramesExceedingThreshold, result.SnrStatistics.FractionOfFramesExceedingThirdSNR,
                                                result.SpectrogramFile.FullName);

                    // It is helpful to opena nd close the output file as we go, so as to keep a record of where we are up to.
                    using (StreamWriter writer = new StreamWriter(opPath, true))
                    {
                        writer.WriteLine(line);
                    }
                } // end while()
            }
            catch (IOException e)
            {
                LoggedConsole.WriteLine("Something went seriously bloody wrong!");
                LoggedConsole.WriteLine(e.ToString());
                return;
            }

            LoggedConsole.WriteLine("fileLocationNotInCsv =" + fileLocationNotInCsv);
            LoggedConsole.WriteLine("fileInCsvDoesNotExist=" + fileInCsvDoesNotExist);
            LoggedConsole.WriteLine("fileExistsCount      =" + fileExistsCount);

            LoggedConsole.WriteLine("\n##### FINISHED FILE ############################\n");
        } // end MAIN()



        public static AudioToSonogramResult AnalyseOneRecording(FileInfo sourceRecording, Dictionary<string, string> configDict, TimeSpan localStart, TimeSpan extractDuration,
                                                                int minHz, int maxHz, DirectoryInfo opDir)
        {
            //int resampleRate = AppConfigHelper.DefaultTargetSampleRate;
            int resampleRate = 22050;
            if (configDict.ContainsKey(AnalysisKeys.ResampleRate))
            {
                resampleRate = Int32.Parse(configDict[AnalysisKeys.ResampleRate]);
            }
            configDict[ConfigKeys.Recording.Key_RecordingCallName] = sourceRecording.FullName;
            configDict[ConfigKeys.Recording.Key_RecordingFileName] = sourceRecording.Name;

            // 1: GET RECORDING and make temporary copy
            // put temp audio FileSegment in same directory as the required output image.
            FileInfo tempAudioSegment = new FileInfo(Path.Combine(opDir.FullName, "tempWavFile.wav"));
            // delete the temp audio file if it already exists.
            if (File.Exists(tempAudioSegment.FullName))
            {
                File.Delete(tempAudioSegment.FullName);
            }
            // This line creates a temporary version of the source file downsampled as per entry in the config file
            MasterAudioUtility.SegmentToWav(sourceRecording, tempAudioSegment, new AudioUtilityRequest() { TargetSampleRate = resampleRate });

            // 2: Generate sonogram image files 
            AudioToSonogramResult result = new AudioToSonogramResult();
            result = Audio2InputForConvCNN.GenerateSpectrogramImages(tempAudioSegment, configDict, opDir);

            // 3: GET the SNR statistics
            result.SnrStatistics = SNR.Calculate_SNR_ShortRecording(tempAudioSegment, configDict, localStart, extractDuration, minHz, maxHz);
            return result;
        }


        /// <summary>
        /// In line class used to store a single record read from a line of the csv file;
        /// </summary>
        public class CsvDataRecord
        {
            public int audio_event_id { get; set; }
            public int audio_recording_id { get; set; }
            //public int audio_recording_uuid { get; set; }
            //public string event_created_at_utc { get; set; }
            public string projects { get; set; }
            public int site_id { get; set; }
            public string site_name { get; set; }
            //event_start_date_utc { get; set; }
            public TimeSpan event_start_seconds { get; set; }
            //event_end_seconds { get; set; }
            public TimeSpan event_duration_seconds { get; set; }
            public int low_frequency_hertz { get; set; }
            public int high_frequency_hertz { get; set; }
            public TimeSpan padding_start_time_seconds { get; set; }
            public TimeSpan padding_end_time_seconds { get; set; }
            public string common_tags { get; set; }
            //species_tags { get; set; }
            //other_tags { get; set; }
            //listen_url { get; set; }
            //library_url { get; set; }
            // path to audio recording
            public FileInfo path { get; set; }
            //download_success { get; set; }
            //skipped { get; set; }

            public static CsvDataRecord ReadLine(string record)
            {
                CsvDataRecord csvDataRecord = new CsvDataRecord();

                // split and parse elements of data line
                var fields = record.Split(',');
                for (int i= 0; i < fields.Length; i++)
                {
                    string word = fields[i];
                    while ((word.StartsWith("\"")) || word.StartsWith(" "))
                    {
                        word = word.Substring(1, word.Length - 1);
                    }
                    while ((word.EndsWith("\"")) || word.EndsWith(" "))
                    {
                        word = word.Substring(0, word.Length - 1);
                    }
                    fields[i] = word;
                }
                csvDataRecord.audio_event_id = Int32.Parse(fields[0]);
                csvDataRecord.audio_recording_id = Int32.Parse(fields[1]);
                csvDataRecord.projects = fields[4];
                csvDataRecord.site_id = Int32.Parse(fields[5]);
                csvDataRecord.site_name = fields[6];

                csvDataRecord.event_start_seconds = TimeSpan.FromSeconds(Double.Parse(fields[8]));
                csvDataRecord.event_duration_seconds = TimeSpan.FromSeconds(Double.Parse(fields[10]));
                csvDataRecord.padding_start_time_seconds = TimeSpan.FromSeconds(Double.Parse(fields[13]));
                csvDataRecord.padding_end_time_seconds   = TimeSpan.FromSeconds(Double.Parse(fields[14]));

                csvDataRecord.low_frequency_hertz = (int)Math.Round(Double.Parse(fields[11]));
                csvDataRecord.high_frequency_hertz = (int)Math.Round(Double.Parse(fields[12]));
                csvDataRecord.common_tags = fields[15];
                csvDataRecord.path = fields[20].ToFileInfo(); 
                return csvDataRecord;
            }
        }
        // class CsvDataRecord



        /// <summary>
        /// In line class used to return results from the static method Audio2InputForConvCNN.GenerateSpectrogramImages();
        /// </summary>
        public class AudioToSonogramResult
        {
            //  path to spectrogram image
            public FileInfo SpectrogramFile { get; set; }
            public SNR.SNRStatistics SnrStatistics { get; set; }
        }

        public static AudioToSonogramResult GenerateSpectrogramImages(FileInfo sourceRecording, Dictionary<string, string> configDict, DirectoryInfo opDir)
        {
            string sourceName = configDict[ConfigKeys.Recording.Key_RecordingFileName];
            sourceName = Path.GetFileNameWithoutExtension(sourceName);

            var result = new AudioToSonogramResult();
            // init the image stack
            var list = new List<Image>();

            // 1) draw amplitude spectrogram
            AudioRecording recordingSegment = new AudioRecording(sourceRecording.FullName);
            SonogramConfig sonoConfig = new SonogramConfig(configDict); // default values config
                
            // disable noise removal for first two spectrograms
            sonoConfig.NoiseReductionType = NoiseReductionType.NONE;

            BaseSonogram sonogram = new AmplitudeSonogram(sonoConfig, recordingSegment.WavReader);
            // remove the DC bin
            sonogram.Data = MatrixTools.Submatrix(sonogram.Data, 0, 1, sonogram.FrameCount - 1, sonogram.Configuration.FreqBinCount);
            //save spectrogram data at this point - prior to noise reduction
            double[,] spectrogramDataBeforeNoiseReduction = sonogram.Data;

            int lowPercentile = 20;
            double neighbourhoodSeconds = 0.25;
            int neighbourhoodFrames = (int)(sonogram.FramesPerSecond * neighbourhoodSeconds);
            double LcnContrastLevel = 0.3;
            //LoggedConsole.WriteLine("LCN: FramesPerSecond (Prior to LCN) = {0}", sonogram.FramesPerSecond);
            //LoggedConsole.WriteLine("LCN: Neighbourhood of {0} seconds = {1} frames", neighbourhoodSeconds, neighbourhoodFrames);
            sonogram.Data = NoiseRemoval_Briggs.NoiseReduction_ShortRecordings_SubtractAndLCN(sonogram.Data, lowPercentile, neighbourhoodFrames, LcnContrastLevel);
            var image = sonogram.GetImageFullyAnnotated("AMPLITUDE SPECTROGRAM + Bin LCN (Local Contrast Normalisation)");
            list.Add(image);
            //string path2 = @"C:\SensorNetworks\Output\Sonograms\dataInput2.png";
            //Histogram.DrawDistributionsAndSaveImage(sonogram.Data, path2);

            double ridgeThreshold = 0.25;
            double[,] matrix = ImageTools.WienerFilter(sonogram.Data, 3);
            byte[,] hits = RidgeDetection.Sobel5X5RidgeDetectionExperiment(matrix, ridgeThreshold);
            hits = RidgeDetection.JoinDisconnectedRidgesInMatrix(hits, matrix, ridgeThreshold);
            image = sonogram.GetColourAmplitudeSpectrogramFullyAnnotated("AMPLITUDE SPECTROGRAM + LCN + ridge detection", spectrogramDataBeforeNoiseReduction, null, hits);
            list.Add(image);


            Image envelopeImage = Image_Track.DrawWaveEnvelopeTrack(recordingSegment, image.Width);
            list.Add(envelopeImage);


            // 2) now draw the standard decibel spectrogram
            sonogram = new SpectrogramStandard(sonoConfig, recordingSegment.WavReader);
            //result.DecibelSpectrogram = (SpectrogramStandard)sonogram;
            image = sonogram.GetImageFullyAnnotated("DECIBEL SPECTROGRAM");
            list.Add(image);

            Image segmentationImage = Image_Track.DrawSegmentationTrack(
                sonogram,
                EndpointDetectionConfiguration.K1Threshold,
                EndpointDetectionConfiguration.K2Threshold,
                image.Width);
            list.Add(segmentationImage);

            // keep the sonogram data (NOT noise reduced) for later use
            double[,] dbSpectrogramData = (double[,])sonogram.Data.Clone();

            // 3) now draw the noise reduced decibel spectrogram
            sonoConfig.NoiseReductionType = NoiseReductionType.STANDARD;
            sonoConfig.NoiseReductionParameter = 3;
            //sonoConfig.NoiseReductionType = NoiseReductionType.SHORT_RECORDING;
            //sonoConfig.NoiseReductionParameter = 50;

            sonogram = new SpectrogramStandard(sonoConfig, recordingSegment.WavReader);
            image = sonogram.GetImageFullyAnnotated("DECIBEL SPECTROGRAM + Lamel noise subtraction");
            list.Add(image);

            // keep the sonogram data for later use
            double[,] nrSpectrogramData = sonogram.Data;

            // 4) A FALSE-COLOUR VERSION OF SPECTROGRAM
            ridgeThreshold = 3.5;
            matrix = ImageTools.WienerFilter(dbSpectrogramData, 3);
            hits = RidgeDetection.Sobel5X5RidgeDetectionExperiment(matrix, ridgeThreshold);

            image = sonogram.GetColourDecibelSpectrogramFullyAnnotated("DECIBEL SPECTROGRAM - Colour annotated", dbSpectrogramData, nrSpectrogramData, hits);
            list.Add(image);

            // 6) COMBINE THE SPECTROGRAM IMAGES
            Image compositeImage = ImageTools.CombineImagesVertically(list);
            FileInfo outputImage = new FileInfo(Path.Combine(opDir.FullName, sourceName + ".png"));
            compositeImage.Save(outputImage.FullName, ImageFormat.Png);
            result.SpectrogramFile = outputImage;

            // 7) Generate the FREQUENCY x OSCILLATIONS Graphs and csv data
            //bool saveData = true;
            //bool saveImage = true;
            //double[] oscillationsSpectrum = Oscillations2014.GenerateOscillationDataAndImages(sourceRecording, configDict, saveData, saveImage);
            return result;
        }

    }


    /// <summary>
    /// This analyzer preprocesses short audio segments a few seconds to maximum 1 minute long for processing by a convolutional Deep NN.
    /// It does not accumulate data or other indices over a long recording.
    /// </summary>
    public class PreprocessorForConvDNN : IAnalyser2
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public PreprocessorForConvDNN()
        {
            this.DisplayName = "ConvolutionalDNN";
            this.Identifier = "Towsey.PreprocessorForConvDNN";
            this.DefaultSettings = new AnalysisSettings()
            {
                SegmentMaxDuration = TimeSpan.FromMinutes(1),
                SegmentMinDuration = TimeSpan.FromSeconds(20),
                SegmentMediaType = MediaTypes.MediaTypeWav,
                SegmentOverlapDuration = TimeSpan.Zero
            };
        }

        public string DisplayName { get; private set; }

        public string Identifier { get; private set; }

        public AnalysisSettings DefaultSettings { get; private set; }

        public AnalysisResult2 Analyse(AnalysisSettings analysisSettings)
        {
            var audioFile = analysisSettings.AudioFile;
            var recording = new AudioRecording(audioFile.FullName);
            var outputDirectory = analysisSettings.AnalysisInstanceOutputDirectory;

            var analysisResult = new AnalysisResult2(analysisSettings, recording.Duration());
            dynamic configuration = Yaml.Deserialise(analysisSettings.ConfigFile);

            bool saveCsv = (bool?)configuration[AnalysisKeys.SaveIntermediateCsvFiles] ?? false;

            if ((bool?)configuration[AnalysisKeys.MakeSoxSonogram] == true)
            {
                Log.Warn("SoX spectrogram generation config variable found (and set to true) but is ignored when running as an IAnalyzer");
            }

            // generate spectrogram
            var configurationDictionary = new Dictionary<string, string>((Dictionary<string, string>)configuration);
            configurationDictionary[ConfigKeys.Recording.Key_RecordingCallName] = audioFile.FullName;
            configurationDictionary[ConfigKeys.Recording.Key_RecordingFileName] = audioFile.Name;
            var spectrogramResult = Audio2Sonogram.GenerateSpectrogramImages(
                audioFile,
                configurationDictionary,
                analysisSettings.AnalysisInstanceOutputDirectory,
                dataOnly: analysisSettings.ImageFile == null,
                makeSoxSonogram: false);

            // this analysis produces no results!
            // but we still print images (that is the point)
            if (analysisSettings.ImageFile != null)
            {
                Debug.Assert(analysisSettings.ImageFile.Exists);
            }

            if (saveCsv)
            {
                var basename = Path.GetFileNameWithoutExtension(analysisSettings.AudioFile.Name);
                var spectrogramCsvFile = outputDirectory.CombineFile(basename + ".Spectrogram.csv");
                Csv.WriteMatrixToCsv(spectrogramCsvFile, spectrogramResult.DecibelSpectrogram.Data, TwoDimensionalArray.RowMajor);
            }

            return analysisResult;
        }

        public void WriteEventsFile(FileInfo destination, IEnumerable<EventBase> results)
        {
            throw new NotImplementedException();
        }

        public void WriteSummaryIndicesFile(FileInfo destination, IEnumerable<SummaryIndexBase> results)
        {
            throw new NotImplementedException();
        }

        public void WriteSpectrumIndicesFiles(DirectoryInfo destination, string fileNameBase, IEnumerable<SpectralIndexBase> results)
        {
            throw new NotImplementedException();
        }

        public SummaryIndexBase[] ConvertEventsToSummaryIndices(
            IEnumerable<EventBase> events,
            TimeSpan unitTime,
            TimeSpan duration,
            double scoreThreshold)
        {
            throw new NotImplementedException();
        }

        public void SummariseResults(
            AnalysisSettings settings,
            FileSegment inputFileSegment,
            EventBase[] events,
            SummaryIndexBase[] indices,
            SpectralIndexBase[] spectralIndices,
            AnalysisResult2[] results)
        {
            // no-op
        }
    }
}

