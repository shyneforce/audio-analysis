﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Audio2Sonogram.cs" company="QutBioacoustics">
//   All code in this file and all associated files are the copyright of the QUT Bioacoustics Research Group (formally MQUTeR).
// </copyright>
// <summary>
//   Defines the Audio2Sonogram type.
//   ACTIVITY CODE: audio2sonogram
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

    public class Audio2Sonogram
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        // use the following paths for the command line for the <audio2sonogram> task. 
        // audio2sonogram "C:\SensorNetworks\WavFiles\LewinsRail\BAC1_20071008-081607.wav" "C:\SensorNetworks\Software\AudioAnalysis\AnalysisConfigFiles\Towsey.Sonogram.cfg"  C:\SensorNetworks\Output\Sonograms\BAC1_20071008-081607.png 0   0  true
        [CustomDetailedDescription]
        [CustomDescription]
        public class Arguments : SourceConfigOutputDirArguments
        {
            public bool Verbose { get; set; }

            [ArgDescription("The start offset (in minutes) of the source audio file to operate on")]
            [ArgRange(0, double.MaxValue)]
            public double? StartOffset { get; set; }

            [ArgDescription("The end offset (in minutes) of the source audio file to operate on")]
            [ArgRange(0, double.MaxValue)]
            public double? EndOffset { get; set; }


            public static string Description()
            {
                return "Generates multiple spectrogram images and ascilllations info";
            }

            public static string AdditionalNotes()
            {
                return "StartOffset and EndOffset are both required when either is included.StartOffset and EndOffset are both required when either is included.StartOffset and EndOffset are both required when either is included.StartOffset and EndOffset are both required when either is included.StartOffset and EndOffset are both required when either is included.StartOffset and EndOffset are both required when either is included.StartOffset and EndOffset are both required when either is included.StartOffset and EndOffset are both required when either is included.StartOffset and EndOffset are both required when either is included.StartOffset and EndOffset are both required when either is included.StartOffset and EndOffset are both required when either is included.";
            }
        }

        private static Arguments Dev()
        {

            return new Arguments
            {
                //Source = @"C:\SensorNetworks\WavFiles\LewinsRail\BAC2_20071008-062040.wav".ToFileInfo(),
                // Source = @"C:\SensorNetworks\WavFiles\LewinsRail\BAC1_20071008-081607.wav".ToFileInfo(),
                //Source = @"C:\SensorNetworks\WavFiles\LewinsRail\BAC2_20071008-085040.wav".ToFileInfo(),

                Source = @"C:\SensorNetworks\WavFiles\Frogs\JCU\Litoria fellax1.mp3".ToFileInfo(),
                //Source = @"C:\SensorNetworks\WavFiles\Frogs\MiscillaneousDataSet\CaneToads_rural1_20_MONO.wav".ToFileInfo(),

                Config = @"C:\Work\GitHub\audio-analysis\AudioAnalysis\AnalysisConfigFiles\Towsey.Sonogram.yml".ToFileInfo(),
                Output = @"C:\SensorNetworks\Output\Sonograms".ToDirectoryInfo(),
                Verbose = true
            };

            throw new NoDeveloperMethodException();
        }


        public static void Main(Arguments arguments)
        {
            if (arguments == null)
            {
                arguments = Dev();
            }

            if (!arguments.Output.Exists) arguments.Output.Create();

            if (arguments.StartOffset.HasValue ^ arguments.EndOffset.HasValue)
            {
                throw new InvalidStartOrEndException("If StartOffset or EndOffset is specified, then both must be specified");
            }

            var offsetsProvided = arguments.StartOffset.HasValue && arguments.EndOffset.HasValue;

            // set default offsets - only use defaults if not provided in argments list
            TimeSpan? startOffset = null;
            TimeSpan? endOffset = null;
            if (offsetsProvided)
            {
                startOffset = TimeSpan.FromMinutes(arguments.StartOffset.Value);
                endOffset   = TimeSpan.FromMinutes(arguments.EndOffset.Value);
            }

            bool verbose = arguments.Verbose;
 

            const string Title = "# MAKE A SONOGRAM FROM AUDIO RECORDING";
            string date  = "# DATE AND TIME: " + DateTime.Now;
            LoggedConsole.WriteLine(Title);
            LoggedConsole.WriteLine(date);
            LoggedConsole.WriteLine("# Input  audio file: " + arguments.Source.Name);


            // 1. set up the necessary files
            FileInfo sourceRecording = arguments.Source;
            FileInfo configFile  = arguments.Config;
            DirectoryInfo opDir  = arguments.Output;

            // 2. get the config dictionary
            dynamic configuration = Yaml.Deserialise(configFile);

            // below three lines are examples of retrieving info from dynamic config
            // string analysisIdentifier = configuration[AnalysisKeys.AnalysisName];
            // bool saveIntermediateWavFiles = (bool?)configuration[AnalysisKeys.SaveIntermediateWavFiles] ?? false;
            // scoreThreshold = (double?)configuration[AnalysisKeys.EventThreshold] ?? scoreThreshold;

            // Resample rate must be 2 X the desired Nyquist. Default is that of recording.
            var resampleRate = (int?)configuration[AnalysisKeys.ResampleRate] ?? AppConfigHelper.DefaultTargetSampleRate;


            var configDict = new Dictionary<string, string>((Dictionary<string, string>)configuration);


            configDict[AnalysisKeys.AddAxes] = ((bool?)configuration[AnalysisKeys.AddAxes] ?? true).ToString();
            configDict[AnalysisKeys.AddSegmentationTrack] = configuration[AnalysisKeys.AddSegmentationTrack] ?? true;

            // # REDUCTION FACTORS for freq and time dimensions
            // #TimeReductionFactor: 1          
            // #FreqReductionFactor: 1

            bool makeSoxSonogram = (bool?)configuration[AnalysisKeys.MakeSoxSonogram] ?? false;
            configDict[AnalysisKeys.SonogramTitle]   = (string)configuration[AnalysisKeys.SonogramTitle] ?? "Sonogram";
            configDict[AnalysisKeys.SonogramComment] = (string)configuration[AnalysisKeys.SonogramComment] ?? "Sonogram produced using SOX";
            configDict[AnalysisKeys.SonogramColored] = (string)configuration[AnalysisKeys.SonogramColored] ?? "false";
            configDict[AnalysisKeys.SonogramQuantisation] = (string)configuration[AnalysisKeys.SonogramQuantisation] ?? "128";

            configDict[ConfigKeys.Recording.Key_RecordingCallName] = arguments.Source.FullName;
            configDict[ConfigKeys.Recording.Key_RecordingFileName] = arguments.Source.Name;

            configDict[AnalysisKeys.AddTimeScale] = (string)configuration[AnalysisKeys.AddTimeScale] ?? "true";
            configDict[AnalysisKeys.AddAxes] = (string)configuration[AnalysisKeys.AddAxes]           ?? "true";
            configDict[AnalysisKeys.AddSegmentationTrack] = (string)configuration[AnalysisKeys.AddSegmentationTrack] ?? "true";
            // ####################################################################
            // SET THE 2 PARAMETERS HERE FOR DETECTION OF OSCILLATION
            // window width when sampling along freq bins
            // 64 is better where many birds and fast chaning activity
            //int sampleLength = 64;
            // 128 is better where slow moving changes to acoustic activity
            int sampleLength = 128;

            // use this if want only dominant oscillations
            //string algorithmName = "Autocorr-SVD-FFT";
            // use this if want more detailed output - but not necessrily accurate!
            string algorithmName = "Autocorr-FFT";
            // tried but not working
            //string algorithmName = "CwtWavelets";
            // ####################################################################



            // print out the sonogram parameters
            if (verbose)
            {
                LoggedConsole.WriteLine("\nPARAMETERS");
                foreach (KeyValuePair<string, string> kvp in configDict)
                {
                    LoggedConsole.WriteLine("{0}  =  {1}", kvp.Key, kvp.Value);
                }
                LoggedConsole.WriteLine("Sample Length for detecting oscillations = {0}", sampleLength);
            }

            // 3: GET RECORDING
            // put temp FileSegment in same directory as the required output image.
            FileInfo tempAudioSegment = new FileInfo(Path.Combine(opDir.FullName, "tempWavFile.wav"));
            // delete the temp audio file if it already exists.
            if (File.Exists(tempAudioSegment.FullName))
            {
                File.Delete(tempAudioSegment.FullName);
            }
            // This line creates a temporary version of the source file downsampled as per entry in the config file
            MasterAudioUtility.SegmentToWav(sourceRecording, tempAudioSegment, new AudioUtilityRequest() { TargetSampleRate = resampleRate });

            // ###### get sonogram image ##############################################################################################
            GenerateSpectrogramImages(tempAudioSegment, configDict, opDir, dataOnly: false, makeSoxSonogram: makeSoxSonogram);           

            LoggedConsole.WriteLine("\n##### FINISHED FILE ###################################################\n");
        }


        /// <summary>
        /// In line class used to return results from the static method Audio2Sonogram.GenerateSpectrogramImages();
        /// </summary>
        public class AudioToSonogramResult
        {
            public SpectrogramStandard DecibelSpectrogram { get; set; }

            //  path to spectrogram image
            public FileInfo SpectrogramImage { get; set; }
            public FileInfo FreqOscillationImage { get; set; }
            public FileInfo FreqOscillationData { get; set; }
        }

        public static AudioToSonogramResult GenerateSpectrogramImages(FileInfo sourceRecording, Dictionary<string, string> configDict, DirectoryInfo opDir, bool dataOnly = false, bool makeSoxSonogram = false)
        {
            string sourceName = configDict[ConfigKeys.Recording.Key_RecordingFileName];
            sourceName = Path.GetFileNameWithoutExtension(sourceName);
            FileInfo outputImage = new FileInfo(Path.Combine(opDir.FullName, sourceName + ".png"));

            var result = new AudioToSonogramResult();

            if (dataOnly && makeSoxSonogram)
            {
                throw new ArgumentException("Can't produce data only for a SoX sonogram");
            }

            if (makeSoxSonogram)
            {
                SpectrogramTools.MakeSonogramWithSox(sourceRecording, configDict, outputImage);
                result.SpectrogramImage = outputImage;
            }
            else if (dataOnly)
            {
                AudioRecording recordingSegment = new AudioRecording(sourceRecording.FullName);
                SonogramConfig sonoConfig = new SonogramConfig(configDict); // default values config

                // disable noise removal
                sonoConfig.NoiseReductionType = NoiseReductionType.NONE;
                Log.Warn("Noise removal disabled!");

                var sonogram = new SpectrogramStandard(sonoConfig, recordingSegment.WavReader);

                result.DecibelSpectrogram = sonogram;
            }
            else
            {
                // init the image stack
                var list = new List<Image>();

                // 1) draw amplitude spectrogram
                AudioRecording recordingSegment = new AudioRecording(sourceRecording.FullName);
                SonogramConfig sonoConfig = new SonogramConfig(configDict); // default values config
                
                // disable noise removal for first two spectrograms
                var disabledNoiseReductionType = sonoConfig.NoiseReductionType;
                sonoConfig.NoiseReductionType = NoiseReductionType.NONE;

                BaseSonogram sonogram = new AmplitudeSonogram(sonoConfig, recordingSegment.WavReader);
                // remove the DC bin
                sonogram.Data = MatrixTools.Submatrix(sonogram.Data, 0, 1, sonogram.FrameCount - 1, sonogram.Configuration.FreqBinCount);

                int neighbourhood = 15;
                double contrastLevel = 0.5;
                sonogram.Data = NoiseRemoval_Briggs.FilterWithLocalColumnVariance(sonogram.Data, neighbourhood, contrastLevel);
                var image = sonogram.GetImageFullyAnnotated("AMPLITUDE SPECTROGRAM + Bin LCN (Local Contrast Normalisation)");
                list.Add(image);
                //string path2 = @"C:\SensorNetworks\Output\Sonograms\dataInput2.png";
                //Histogram.DrawDistributionsAndSaveImage(sonogram.Data, path2);

                Image envelopeImage = Image_Track.DrawWaveEnvelopeTrack(recordingSegment, image.Width);
                list.Add(envelopeImage);

                // 2) now draw the standard decibel spectrogram
                sonogram = new SpectrogramStandard(sonoConfig, recordingSegment.WavReader);
                result.DecibelSpectrogram = (SpectrogramStandard)sonogram;
                image = sonogram.GetImageFullyAnnotated("DECIBEL SPECTROGRAM");
                list.Add(image);

                Image segmentationImage = Image_Track.DrawSegmentationTrack(
                    sonogram,
                    EndpointDetectionConfiguration.K1Threshold,
                    EndpointDetectionConfiguration.K2Threshold,
                    image.Width);
                list.Add(segmentationImage);

                // keep the sonogram data for later use
                double[,] dbSpectrogramData = (double[,])sonogram.Data.Clone();

                // 3) now draw the noise reduced decibel spectrogram
                // #NOISE REDUCTION PARAMETERS - restore noise reduction
                sonoConfig.NoiseReductionType = disabledNoiseReductionType;
                sonoConfig.NoiseReductionParameter = double.Parse(configDict[AnalysisKeys.NoiseBgThreshold] ?? "3.0");

                sonogram = new SpectrogramStandard(sonoConfig, recordingSegment.WavReader);
                image = sonogram.GetImageFullyAnnotated("DECIBEL SPECTROGRAM + Lamel noise subtraction");
                list.Add(image);

                // keep the sonogram data for later use
                double[,] nrSpectrogramData = sonogram.Data;

                // 4) A FALSE-COLOUR VERSION OF SPECTROGRAM
                image = sonogram.GetColourSpectrogramFullyAnnotated("DECIBEL SPECTROGRAM - Colour annotated", dbSpectrogramData, nrSpectrogramData);
                list.Add(image);

                // 5) TODO: ONE OF THESE YEARS FIX UP THE CEPTRAL SONOGRAM
                ////SpectrogramCepstral cepgram = new SpectrogramCepstral((AmplitudeSonogram)amplitudeSpg);
                ////var mti3 = SpectrogramTools.Sonogram2MultiTrackImage(sonogram, configDict);
                ////var image3 = mti3.GetImage();
                ////image3.Save(fiImage.FullName + "3", ImageFormat.Png);


                // 6) COMBINE THE SPECTROGRAM IMAGES
                Image compositeImage = ImageTools.CombineImagesVertically(list);
                compositeImage.Save(outputImage.FullName, ImageFormat.Png);
                result.SpectrogramImage = outputImage;
            }

            // 7) Generate the FREQUENCY x OSCILLATIONS Graphs and csv
            FileInfo tempAudioSegment = sourceRecording;
            Oscillations2014.FreqVsOscillationsResult result2 = GenerateOscillationImages(tempAudioSegment, configDict, dataOnly = false);

            FileInfo outputOscImage = new FileInfo(Path.Combine(opDir.FullName, sourceName + ".FreqOscilMatrix.png"));
            FileInfo outputOscCsv   = new FileInfo(Path.Combine(opDir.FullName, sourceName + ".FreqOscilMatrix.csv"));

            // Save the image and csv files
            result2.FreqOscillationImage.Save(outputOscImage.FullName, ImageFormat.Png);
            Acoustics.Shared.Csv.Csv.WriteMatrixToCsv(outputOscCsv, result2.FreqOscillationData);
             // return paths to saved files
            result.FreqOscillationData  = outputOscCsv;
            result.FreqOscillationImage = outputOscImage;
            return result;
        }

        /// <summary>
        /// Generates the FREQUENCY x OSCILLATIONS Graphs and csv
        /// </summary>
        /// <param name="sourceRecording"></param>
        /// <param name="tempAudioSegment"></param>
        /// <param name="configDict"></param>
        /// <param name="opDir"></param>
        /// <param name="dataOnly"></param>
        /// <returns></returns>
        public static Oscillations2014.FreqVsOscillationsResult GenerateOscillationImages(FileInfo tempAudioSegment, Dictionary<string, string> configDict, 
                                                                                          bool dataOnly = false)
        {
            SonogramConfig sonoConfig = new SonogramConfig(configDict); // default values config
            sonoConfig.WindowSize = 256;
            AudioRecording recordingSegment = new AudioRecording(tempAudioSegment.FullName);
            BaseSonogram sonogram = new AmplitudeSonogram(sonoConfig, recordingSegment.WavReader);
            Console.WriteLine("Oscillation Detection: FramesPerSecond = {0}", sonogram.FramesPerSecond);
            // remove the DC bin
            sonogram.Data = MatrixTools.Submatrix(sonogram.Data, 0, 1, sonogram.FrameCount - 1, sonogram.Configuration.FreqBinCount);

            // LocalContrastNormalisation over frequency bins is better and faster.
            int neighbourhood = 15;
            double contrastLevel = 0.5;
            sonogram.Data = NoiseRemoval_Briggs.FilterWithLocalColumnVariance(sonogram.Data, neighbourhood, contrastLevel);

            Oscillations2014.SensitivityThreshold = 0.4;
            Oscillations2014.SampleLength = 128;

            var resultsList = new List<Oscillations2014.FreqVsOscillationsResult>();
            resultsList.Add(Oscillations2014.GetFreqVsOscillationsDataAndImage(sonogram, "Autocorr-FFT"));
            resultsList.Add(Oscillations2014.GetFreqVsOscillationsDataAndImage(sonogram, "Autocorr-SVD-FFT"));

            // store combined data in the same results class
            var result = new Oscillations2014.FreqVsOscillationsResult();
            result.FreqOscillationData = resultsList[1].FreqOscillationData;
            if (dataOnly)
            {
                return result;
            }
            else
            {
                Image freOscCompositeImage = Oscillations2014.FreqVsOscillationsResult.CombineImages(resultsList);
                result.FreqOscillationImage = freOscCompositeImage;
            }
            return result;
        }


    }


    /// <summary>
    /// This analyzer simply generates spectrograms and outputs them to CSV files.
    /// </summary>
    public class SpectrogramAnalyzer : IAnalyser2
    {
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        public SpectrogramAnalyzer()
        {
            this.DisplayName = "Spectrogram Analyzer";
            this.Identifier = "Towsey.SpectrogramGenerator";
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


