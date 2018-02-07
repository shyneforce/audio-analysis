﻿namespace AnalysisPrograms
{

    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Data;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Acoustics.Shared;
    using Acoustics.Shared.Contracts;
    using Acoustics.Shared.Extensions;
    using Acoustics.Tools;
    using Acoustics.Tools.Audio;
    using AnalysisBase;
    using Production;
    using AudioAnalysisTools;
    using AudioAnalysisTools.DSP;
    using AudioAnalysisTools.Indices;
    using AudioAnalysisTools.StandardSpectrograms;
    using AudioAnalysisTools.WavTools;
    using TowseyLibrary;

    [Obsolete("This code does not work. It should be ported to be a modern recognizer")]
    public class LSKiwi3
    {
        public class Arguments : AnalyserArguments
        {
        }

        //CONSTANTS
        public const string AnalysisName = "LSKiwi3";
        public const int ResampleRate = 17640;

        public string DisplayName
        {
            get { return "Little Spotted Kiwi v3"; }
        }

        public string Identifier
        {
            get { return "Towsey." + AnalysisName; }
        }

        [Obsolete("See https://github.com/QutBioacoustics/audio-analysis/issues/134")]
        public static void Dev(Arguments arguments)
        {
            var executeDev = arguments == null;
            if (executeDev)
            {
                //Following lines are used for the debug command line.
                // kiwi "C:\SensorNetworks\WavFiles\Kiwi\Samples\lsk-female\TOWER_20091107_07200_21.LSK.F.wav"  "C:\SensorNetworks\WavFiles\Kiwi\Samples\lskiwi_Params.txt"
                // kiwi "C:\SensorNetworks\WavFiles\Kiwi\Samples\lsk-male\TOWER_20091112_072000_25.LSK.M.wav"  "C:\SensorNetworks\WavFiles\Kiwi\Samples\lskiwi_Params.txt"
                // kiwi "C:\SensorNetworks\WavFiles\Kiwi\TUITCE_20091215_220004_Cropped.wav"     "C:\SensorNetworks\WavFiles\Kiwi\Samples\lskiwi_Params.txt"
                // 8 min test recording  // kiwi "C:\SensorNetworks\WavFiles\Kiwi\TUITCE_20091215_220004_CroppedAnd2.wav" "C:\SensorNetworks\WavFiles\Kiwi\Results_MixedTest\lskiwi_Params.txt"
                // kiwi "C:\SensorNetworks\WavFiles\Kiwi\KAPITI2_20100219_202900.wav"   "C:\SensorNetworks\WavFiles\Kiwi\Results\lskiwi_Params.txt"
                // kiwi "C:\SensorNetworks\WavFiles\Kiwi\TOWER_20100208_204500.wav"     "C:\SensorNetworks\WavFiles\Kiwi\Results_TOWER_20100208_204500\lskiwi_Params.txt"

                string configPath = @"C:\SensorNetworks\Software\AudioAnalysis\AnalysisConfigFiles\Towsey.LSKiwi3.cfg";

                //string ANDREWS_SELECTION_PATH = @"C:\SensorNetworks\WavFiles\Kiwi\Results_TOWER_20100208_204500\TOWER_20100208_204500_ANDREWS_SELECTIONS.csv";
                string recordingPath = @"C:\SensorNetworks\WavFiles\Kiwi\TOWER_20100208_204500.wav";
                //string recordingPath = @"C:\SensorNetworks\WavFiles\Kiwi\TOWER_20100208_204500_40m0s.wav";
                string outputDir = @"C:\SensorNetworks\Output\LSKiwi3\Dev\";

                //string recordingPath = @"C:\SensorNetworks\WavFiles\Kiwi\KAPITI2_20100219_202900.wav";
                //string recordingPath = @"C:\SensorNetworks\WavFiles\Kiwi\KAPITI2-20100219-202900_Airplane.mp3";
                //string outputDir     = @"C:\SensorNetworks\Output\Kiwi\KAPITI2_20100219_202900\";
                //string ANDREWS_SELECTION_PATH = @"C:\SensorNetworks\WavFiles\Kiwi\Results_KAPITI2_20100219_202900\KAPITI2_20100219_202900_ANDREWS_SELECTIONS.csv";

                //string recordingPath = @"C:\SensorNetworks\WavFiles\Kiwi\TUITCE_20091215_220004.wav";
                //string outputDir     = @"C:\SensorNetworks\Output\Kiwi\TUITCE_20091215_220004\";
                //string ANDREWS_SELECTION_PATH = @"C:\SensorNetworks\WavFiles\Kiwi\Results_TUITCE_20091215_220004\TUITCE_20091215_220004_ANDREWS_SELECTIONS.csv";

                //string recordingPath = @"C:\SensorNetworks\WavFiles\Kiwi\TUITCE_20091215_210000.wav";
                //string outputDir     = @"C:\SensorNetworks\Output\LSKiwi2\TuiTce_20091215_210000\";
                //string ANDREWS_SELECTION_PATH = @"C:\SensorNetworks\WavFiles\Kiwi\Results_TUITCE_20091215_210000\TUITCE_20091215_210000_ANDREWS_SELECTIONS.csv";

                string title = "# FOR DETECTION OF THE LITTLE SPOTTED KIWI - version 3 - started 23rd June 2012";
                string date = "# DATE AND TIME: " + DateTime.Now;
                LoggedConsole.WriteLine(title);
                LoggedConsole.WriteLine(date);
                LoggedConsole.WriteLine("# Output folder:  " + outputDir);
                LoggedConsole.WriteLine("# Recording file: " + Path.GetFileName(recordingPath));
                var diOutputDir = new DirectoryInfo(outputDir);

                //Log.Verbosity = 1;
                int startMinute = 40;
                int durationSeconds = 300; //set zero to get entire recording
                var tsStart = new TimeSpan(0, startMinute, 0); //hours, minutes, seconds
                var tsDuration = new TimeSpan(0, 0, durationSeconds); //hours, minutes, seconds
                var segmentFileStem = Path.GetFileNameWithoutExtension(recordingPath);
                var segmentFName = string.Format("{0}_{1}min.wav", segmentFileStem, startMinute);
                var sonogramFname = string.Format("{0}_{1}min.png", segmentFileStem, startMinute);
                var eventsFname = string.Format("{0}_{1}min.{2}.Events.csv", segmentFileStem, startMinute, "Towsey." + AnalysisName);
                var indicesFname = string.Format("{0}_{1}min.{2}.Indices.csv", segmentFileStem, startMinute, "Towsey." + AnalysisName);

                //var fiCsvFile = new FileInfo(restOfArgs[0]);
                //var fiConfigFile = new FileInfo(restOfArgs[1]);
                //IAnalysis analyser = new LSKiwi2();
                //analyser.ProcessCsvFile(fiCsvFile, fiConfigFile); //returns a datatable which has no relevance at this level.

                arguments = new Arguments
                {
                    Source = recordingPath.ToFileInfo(),
                    Config = configPath.ToFileInfo(),
                    Output = outputDir.ToDirectoryInfo(),
                    Start = tsStart.TotalSeconds,
                    Duration = tsDuration.TotalSeconds,
                };
            }

            Execute(arguments);
        }

        /// <summary>
        /// A WRAPPER AROUND THE analyser.Analyze(analysisSettings) METHOD
        /// To be called as an executable with command line arguments.
        /// </summary>
        public static void Execute(Arguments arguments)
        {
            Contract.Requires(arguments != null);

            throw new NotImplementedException("This code is no longer supported");
            /*
            AnalysisSettings analysisSettings = arguments.ToAnalysisSettings();
            TimeSpan tsStart = TimeSpan.FromSeconds(arguments.Start ?? 0);
            TimeSpan tsDuration = TimeSpan.FromSeconds(arguments.Duration ?? 0);

            //EXTRACT THE REQUIRED RECORDING SEGMENT
            FileInfo tempF = analysisSettings.SegmentSettings.SegmentAudioFile;
            if (tsDuration == TimeSpan.Zero)   //Process entire file
            {
                AudioFilePreparer.PrepareFile(arguments.Source, tempF, new AudioUtilityRequest { TargetSampleRate = ResampleRate }, analysisSettings.AnalysisTempDirectoryFallback);
                //var fiSegment = AudioFilePreparer.PrepareFile(diOutputDir, fiSourceFile, , Human2.RESAMPLE_RATE);
            }
            else
            {
                AudioFilePreparer.PrepareFile(arguments.Source, tempF, new AudioUtilityRequest { TargetSampleRate = ResampleRate, OffsetStart = tsStart, OffsetEnd = tsStart.Add(tsDuration) }, analysisSettings.AnalysisTempDirectoryFallback);
                //var fiSegmentOfSourceFile = AudioFilePreparer.PrepareFile(diOutputDir, new FileInfo(recordingPath), MediaTypes.MediaTypeWav, TimeSpan.FromMinutes(2), TimeSpan.FromMinutes(3), RESAMPLE_RATE);
            }

            //DO THE ANALYSIS
            //#############################################################################################################################################
            IAnalyser analyser = new LSKiwi3();
            AnalysisResult result = analyser.Analyse(analysisSettings);
            DataTable dt = result.Data;
            //#############################################################################################################################################

            //ADD IN ADDITIONAL INFO TO RESULTS TABLE
            if (dt != null)
            {
                AddContext2Table(dt, tsStart, result.AudioDuration);
                CsvTools.DataTable2CSV(dt, analysisSettings.SegmentSettings.SegmentEventsFile.FullName);
                //DataTableTools.WriteTable(augmentedTable);
            }
            */
        }

        private static readonly object imageWriteLock = new object();

        public AnalysisResult Analyse(AnalysisSettings analysisSettings)
        {
            throw new NotImplementedException("This code is no longer supported");
            /*
            //var configuration = new ConfigDictionary(analysisSettings.ConfigFile.FullName);
            //Dictionary<string, string> configDict = configuration.GetTable();
            var fiAudioF = analysisSettings.SegmentSettings.SegmentAudioFile;
            var diOutputDir = analysisSettings.SegmentSettings.SegmentOutputDirectory;

            var analysisResults = new AnalysisResult();
            analysisResults.AnalysisIdentifier = this.Identifier;
            analysisResults.SettingsUsed = analysisSettings;
            analysisResults.Data = null;

            //######################################################################
            var results = Analysis(fiAudioF, analysisSettings);
            //######################################################################

            if (results == null) return analysisResults; //nothing to process
            var sonogram = results.Item1;
            var hits = results.Item2;
            var scores = results.Item3;
            var predictedEvents = results.Item4;
            var recordingTimeSpan = results.Item5;
            analysisResults.AudioDuration = recordingTimeSpan;

            DataTable dataTableOfEvents = null;

            if ((predictedEvents != null) && (predictedEvents.Count != 0))
            {
                string analysisName = analysisSettings.ConfigDict[AnalysisKeys.AnalysisName];
                string fName = Path.GetFileNameWithoutExtension(fiAudioF.Name);
                foreach (AcousticEvent ev in predictedEvents)
                {
                    ev.FileName = fName;
                    //ev.Name = analysisName; //name was added previously
                    ev.SegmentDuration = recordingTimeSpan;
                }
                //write events to a data table to return.
                dataTableOfEvents = WriteEvents2DataTable(predictedEvents);
                string sortString = AnalysisKeys.EventStartSec + " ASC";
                dataTableOfEvents = DataTableTools.SortTable(dataTableOfEvents, sortString); //sort by start time before returning
            }

            if ((analysisSettings.SegmentSettings.SegmentEventsFile != null) && (dataTableOfEvents != null))
            {
                CsvTools.DataTable2CSV(dataTableOfEvents, analysisSettings.SegmentSettings.SegmentEventsFile.FullName);
            }

            if ((analysisSettings.SegmentSettings.SegmentSummaryIndicesFile != null) && (dataTableOfEvents != null))
            {
                double eventThreshold = ConfigDictionary.GetDouble(AnalysisKeys.EventThreshold, analysisSettings.ConfigDict);
                TimeSpan unitTime = TimeSpan.FromSeconds(60); //index for each time span of one minute
                var indicesDT = this.ConvertEvents2Indices(dataTableOfEvents, unitTime, recordingTimeSpan, eventThreshold);
                CsvTools.DataTable2CSV(indicesDT, analysisSettings.SegmentSettings.SegmentSummaryIndicesFile.FullName);
            }

            //save image of sonograms
            if ((sonogram != null) && (analysisSettings.AnalysisImageSaveBehavior.ShouldSave(analysisResults.Data.Rows.Count)))
            {
                var fileExists = File.Exists(analysisSettings.SegmentSettings.SegmentImageFile.FullName);
                string imagePath = analysisSettings.SegmentSettings.SegmentImageFile.FullName;
                double eventThreshold = 0.1;
                Image image = DrawSonogram(sonogram, hits, scores, predictedEvents, eventThreshold);

                image.Save(analysisSettings.SegmentSettings.SegmentImageFile.FullName, ImageFormat.Png);
            }

            analysisResults.Data = dataTableOfEvents;
            analysisResults.ImageFile = analysisSettings.SegmentSettings.SegmentImageFile;
            analysisResults.AudioDuration = recordingTimeSpan;
            //result.DisplayItems = { { 0, "example" }, { 1, "example 2" }, }
            //result.OutputFiles = { { "exmaple file key", new FileInfo("Where's that file?") } }
            return analysisResults;
            */
        } //Analyze()

        /// <summary>
        /// ################ THE KEY ANALYSIS METHOD
        /// Returns a DataTable
        /// </summary>
        /// <param name="fiSegmentOfSourceFile"></param>
        /// <param name="analysisSettings"></param>
        /// <param name="segmentStartOffset"></param>
        /// <param name="configDict"></param>
        /// <param name="diOutputDir"></param>
        public static Tuple<BaseSonogram, double[,], List<Plot>, List<AcousticEvent>, TimeSpan> Analysis(FileInfo fiSegmentOfSourceFile, AnalysisSettings analysisSettings, TimeSpan segmentStartOffset)
        {
            Dictionary<string, string> config = analysisSettings.ConfigDict;

            int minHzMale = ConfigDictionary.GetInt(LSKiwiHelper.key_MIN_HZ_MALE, config);
            int maxHzMale = ConfigDictionary.GetInt(LSKiwiHelper.key_MAX_HZ_MALE, config);
            int minHzFemale = ConfigDictionary.GetInt(LSKiwiHelper.key_MIN_HZ_FEMALE, config);
            int maxHzFemale = ConfigDictionary.GetInt(LSKiwiHelper.key_MAX_HZ_FEMALE, config);
            int frameLength = ConfigDictionary.GetInt(AnalysisKeys.FrameLength, config);
            double frameOverlap = ConfigDictionary.GetDouble(AnalysisKeys.FrameOverlap, config);
            double minPeriod = ConfigDictionary.GetDouble(AnalysisKeys.MinPeriodicity, config);
            double maxPeriod = ConfigDictionary.GetDouble(AnalysisKeys.MaxPeriodicity, config);
            double eventThreshold = ConfigDictionary.GetDouble(AnalysisKeys.EventThreshold, config);
            double minDuration = ConfigDictionary.GetDouble(AnalysisKeys.MinDuration, config); //minimum event duration to qualify as species call
            double maxDuration = ConfigDictionary.GetDouble(AnalysisKeys.MaxDuration, config); //maximum event duration to qualify as species call

            //get weights to derive combo score and rules to filter events
            var configPath = analysisSettings.ConfigFile;
            var weights = LSKiwiHelper.GetFeatureWeights(configPath.FullName);
            bool doFilterEvents = ConfigDictionary.GetBoolean(LSKiwiHelper.key_FILTER_EVENTS, config);

            AudioRecording recording = new AudioRecording(fiSegmentOfSourceFile.FullName);
            if (recording == null)
            {
                LoggedConsole.WriteLine("AudioRecording == null. Analysis not possible.");
                return null;
            }
            TimeSpan tsRecordingtDuration = recording.Duration;
            if (tsRecordingtDuration.TotalSeconds < 15)
            {
                LoggedConsole.WriteLine("Audio recording must be atleast 15 seconds long for analysis.");
                return null;
            }

            //i: MAKE SONOGRAM
            SonogramConfig sonoConfig = new SonogramConfig(); //default values config
            sonoConfig.SourceFName = recording.BaseName;
            sonoConfig.WindowSize = frameLength;
            sonoConfig.WindowOverlap = frameOverlap;
            sonoConfig.NoiseReductionType = NoiseReductionType.Standard; //MUST DO NOISE REMOVAL - XCORR only works well if do noise removal
            BaseSonogram sonogram = new SpectrogramStandard(sonoConfig, recording.WavReader);

            //DETECT MALE KIWI
            var resultsMale = DetectKiwi(sonogram, minHzMale, maxHzMale, minPeriod, maxPeriod, eventThreshold, minDuration, maxDuration, weights, segmentStartOffset);
            var scoresM = resultsMale.Item1;
            var hitsM = resultsMale.Item2;
            var predictedEventsM = resultsMale.Item3;
            foreach (AcousticEvent ev in predictedEventsM) ev.Name = "LSK(m)";

            //DETECT FEMALE KIWI
            var resultsFemale = DetectKiwi(sonogram, minHzFemale, maxHzFemale, minPeriod, maxPeriod, eventThreshold, minDuration, maxDuration, weights, segmentStartOffset);
            var scoresF = resultsFemale.Item1;
            var hitsF = resultsFemale.Item2;
            var predictedEventsF = resultsFemale.Item3;
            foreach (AcousticEvent ev in predictedEventsF) ev.Name = "LSK(f)";

            //combine the male and female results
            hitsM = MatrixTools.AddMatrices(hitsM, hitsF);
            foreach (AcousticEvent ev in predictedEventsF) predictedEventsM.Add(ev);

            //filter events if requested but this can be done by See5.0
            if (doFilterEvents)
            {
                var filterRules = LSKiwiHelper.GetExcludeRules(configPath.FullName);
                for (int i = 0; i < predictedEventsM.Count; i++)
                {
                    predictedEventsM[i] = LSKiwiHelper.FilterEvent(predictedEventsM[i], filterRules);
                }
            }

            scoresM.Add(new Plot(" ", new double[sonogram.FrameCount], 0.0)); //just add a spacer track for convenience.
            foreach (Plot plot in scoresF) scoresM.Add(plot);

            return Tuple.Create(sonogram, hitsM, scoresM, predictedEventsM, tsRecordingtDuration);
        } //Analysis()

        public static Tuple<List<Plot>, double[,], List<AcousticEvent>> DetectKiwi(
            BaseSonogram sonogram,
            int minHz,
            int maxHz,
            double minPeriod,
            double maxPeriod,
            double eventThreshold,
            double minDuration,
            double maxDuration,
            Dictionary<string, double> weights,
            TimeSpan segmentStartOffset)
        {
            int step = (int)Math.Round(sonogram.FramesPerSecond); //take one second steps
            //#############################################################################################################################################
            //                                                          (---frame duration --)
            //window    sr          frameDuration   frames/sec  hz/bin   32       64      128      hz/64bins       hz/128bins
            // 1024     22050       46.4ms            21.5      21.5            2944ms              1376hz          2752hz
            // 1024     17640       58.0ms            17.2      17.2    1.85s   3.715s    7.42s     1100hz          2200hz  More than 3s is too long a sample - require stationarity.
            // 2048     17640       116.1ms            8.6       8.6    3.71s   7.430s   14.86s      551hz          1100hz
            //@frame size = 1024:
            //@frame size = 2048: 32 frames = 1.85 seconds.   64 frames  (i.e. 3.7 seconds) is to long a sample - require stationarity.
            int sampleLength = 64; //assuming window = 1024
            if (sonogram.Configuration.WindowSize == 2048) sampleLength = 32;

            int rowCount = sonogram.Data.GetLength(0);
            int colCount = sonogram.Data.GetLength(1);
            double minFramePeriod = minPeriod * sonogram.FramesPerSecond;
            double maxFramePeriod = maxPeriod * sonogram.FramesPerSecond;

            int minBin = (int)(minHz / sonogram.FBinWidth);
            int maxBin = (int)(maxHz / sonogram.FBinWidth);
            int bandWidth = (int)((maxHz - minHz) / sonogram.FBinWidth);

            double[,] subMatrix = MatrixTools.Submatrix(sonogram.Data, 0, minBin, (rowCount - 1), minBin + bandWidth);
            double[] dBArray = MatrixTools.GetRowAverages(subMatrix);
            var result1 = AutoAndCrossCorrelation.DetectXcorrelationInTwoArrays(dBArray, dBArray, step, sampleLength, minFramePeriod, maxFramePeriod);
            double[] intensity1 = result1.Item1;
            double[] periodicity1 = result1.Item2;
            intensity1 = DataTools.filterMovingAverage(intensity1, 5);
            //#############################################################################################################################################

            //iii MAKE array showing locations of dB peaks and periodicity at that point
            bool[] peaks = DataTools.GetPeaks(dBArray);
            double[] peakPeriodicity = new double[dBArray.Length];
            for (int r = 0; r < dBArray.Length; r++)
            {
                if ((peaks[r]) && (intensity1[r] > eventThreshold))
                {
                    peakPeriodicity[r] = periodicity1[r];
                }
            }

            double[] gridScore = CalculateGridScore(dBArray, peakPeriodicity);
            double[] deltaPeriodScore = CalculateDeltaPeriodScore(periodicity1, sonogram.FramesPerSecond);
            double[] chirps = CalculateKiwiChirpScore(dBArray, peakPeriodicity, subMatrix);
            double[] chirpScores = ConvertChirpsToScoreArray(chirps, dBArray, sonogram.FramesPerSecond);
            double[] bandWidthScore = CalculateKiwiBandWidthScore(sonogram, minHz, maxHz, peakPeriodicity);

            //GET A COMBINED WEIGHTED SCORE FOR FOUR OF THE FEATURES
            double[] comboScore = new double[dBArray.Length];
            for (int r = 0; r < dBArray.Length; r++)
            {
                comboScore[r] = (intensity1[r] * weights[LSKiwiHelper.key_INTENSITY_SCORE]) +
                                (gridScore[r] * weights[LSKiwiHelper.key_GRID_SCORE]) +
                                (deltaPeriodScore[r] * weights[LSKiwiHelper.key_DELTA_SCORE]) +
                                (chirpScores[r] * weights[LSKiwiHelper.key_CHIRP_SCORE]);    //weighted sum
            }

            //iii: CONVERT SCORES TO ACOUSTIC EVENTS
            var events = ConvertScoreArray2Events(dBArray, intensity1, gridScore, deltaPeriodScore, chirpScores, comboScore, bandWidthScore,
                                                          minHz, maxHz, sonogram.FramesPerSecond, sonogram.FBinWidth,
                                                          eventThreshold, minDuration, maxDuration,
                segmentStartOffset);

            //double minGap = 10.0; //seconds
            //MergeEvents(events, minGap);    //decide not to use this
            CropEvents(events, dBArray, minDuration);

            // PREPARE HITS MATRIX - now do not do because computaionally expensive and makes songrams a bit difficult to interpret
            double[,] hits = null;
            //var hits = new double[rowCount, colCount];
            //double range = maxFramePeriod - minFramePeriod;
            //for (int r = 0; r < rowCount; r++)
            //{
            //    if (intensity1[r] > eventThreshold)
            //    for (int c = minBin; c < maxBin; c++)
            //    {
            //        hits[r, c] = (periodicity1[r] - minFramePeriod) / range; //normalisation
            //    }
            //}

            var scores = new List<Plot>();
            scores.Add(new Plot("Decibels", DataTools.normalise(dBArray), 0.0));
            scores.Add(new Plot("Xcorrelation score", DataTools.normalise(intensity1), 0.0));
            //scores.Add(new Plot("Period Normed",      DataTools.NormaliseMatrixValues(periodicity1), 0.0));
            scores.Add(new Plot("Delta Period Score", DataTools.normalise(deltaPeriodScore), 0.0));
            scores.Add(new Plot("Grid Score", DataTools.normalise(gridScore), 0.0));
            scores.Add(new Plot("Chirps", chirps, 0.5));
            scores.Add(new Plot("Chirp Score", chirpScores, 0.0));
            scores.Add(new Plot("Bandwidth Score", bandWidthScore, 0.5));
            scores.Add(new Plot("Combo Score", comboScore, eventThreshold));
            return Tuple.Create(scores, hits, events);
        }

        //public static double[] NormalisePeriodicity(double[] periodicity, double minFramePeriod, double maxFramePeriod)
        //{
        //    double range = maxFramePeriod - minFramePeriod;
        //    for (int i = 0; i < periodicity.Length; i++)
        //    {
        //        //if (i > 100)
        //        //    LoggedConsole.WriteLine("{0}      {1}",  periodicity[i], ((periodicity[i] - minFramePeriod) / range));
        //        if (periodicity[i] <= 0.0) continue;
        //        periodicity[i] = (periodicity[i] - minFramePeriod) / range;
        //    }
        //    return periodicity;
        //}

        public static double[] CalculateGridScore(double[] dBArray, double[] peakPeriodicity)
        {
            int length = dBArray.Length;
            var gridScore = new double[length];
            int numberOfCycles = 4;

            for (int i = 0; i < peakPeriodicity.Length; i++)
            {
                if (peakPeriodicity[i] <= 0.0) continue;
                //calculate grid score

                int cyclePeriod = (int)peakPeriodicity[i];
                int minPeriod = cyclePeriod - 2;
                int maxPeriod = cyclePeriod + 2;
                double score = 0.0;
                int scoreLength = 0;
                for (int p = minPeriod; p <= maxPeriod; p++)
                {
                    int segmentLength = numberOfCycles * p;
                    double[] extract = DataTools.Subarray(dBArray, i, segmentLength);
                    if (extract.Length != segmentLength) return gridScore; // reached end of array

                    double[] reducedSegment = Gratings.ReduceArray(extract, p, numberOfCycles);
                    double pScore = Gratings.DetectPeriod2Grating(reducedSegment);
                    if (pScore > score) // only store the maximum score achieved over the different periods
                    {
                        score = pScore;
                        scoreLength = segmentLength;
                    }
                }

                //transfer score to output array - overwrite previous values if they were smaller.
                for (int x = 0; x < scoreLength; x++)
                    if (gridScore[i + x] < score) gridScore[i + x] = score;
            }
            return gridScore;
        }

        //Delta score is number of frames over which there is a gradual increase in period.
        public static double[] CalculateDeltaPeriodScore(double[] periodicity, double framesPerSecond)
        {
            int maxSeconds = 20;
            double maxCount = maxSeconds * framesPerSecond; //use as a normalising factor
            double[] deltaScore = new double[periodicity.Length];
            int count = 0;
            int start = 0;
            double startPeriodicity = 0.0;
            for (int i = 1; i < periodicity.Length; i++)
            {
                count++;
                //IF there is drop in period AND periodicity has increased THEN calculate a delta score
                if (periodicity[i] < periodicity[i - 1])
                {
                    if (periodicity[i - 1] > startPeriodicity)
                    {
                        double score = count / maxCount;
                        if (score > 1.0) score = 1.0;
                        for (int j = start; j < i; j++)
                        {
                            deltaScore[j] = score;
                        }
                    }
                    count = 0;
                    start = i;
                    startPeriodicity = periodicity[i];
                }
            }
            return deltaScore;
        }

        public static double[] CalculateKiwiChirpScore(double[] dBArray, double[] peakPeriodicity, double[,] matrix)
        {
            int length = dBArray.Length;
            double[] chirpScore = new double[length];
            for (int i = 1; i < dBArray.Length - 1; i++)
            {
                if (peakPeriodicity[i] == 0.0) continue;
                //have a peak get spectra before and after
                double[] spectrumM1 = MatrixTools.GetRow(matrix, i - 1);
                double[] spectrumP1 = MatrixTools.GetRow(matrix, i + 1);
                spectrumM1 = DataTools.filterMovingAverage(spectrumM1, 5);
                spectrumP1 = DataTools.filterMovingAverage(spectrumP1, 5);
                double[] peakValuesM1 = DataTools.GetPeakValues(spectrumM1);
                double[] peakValuesP1 = DataTools.GetPeakValues(spectrumP1);
                int[] peakLocationsM1 = DataTools.GetOrderedPeakLocations(peakValuesM1, 2);
                int[] peakLocationsP1 = DataTools.GetOrderedPeakLocations(peakValuesP1, 2);
                double avLocationM1 = peakLocationsM1.Average();
                double avLocationP1 = peakLocationsP1.Average();
                double difference = avLocationP1 - avLocationM1;
                double normalisingRange = 100.0; //80 hz is max
                if (difference < 0.0) chirpScore[i] = 0.0;
                else if (difference > normalisingRange) chirpScore[i] = 1.0;
                else chirpScore[i] = difference / normalisingRange;
            }
            return chirpScore;
        }

        //returns a continuous average chirp score per second derived from the density and intensity of chirps
        //public static double[] ConvertChirpsToScoreArray(double[] chirps, double[] dBArray, double framesPerSecond)
        //{
        //    int length = chirps.Length;
        //    double[] chirpScores = new double[length];
        //    int secondsSpan = 5; //calculate the chirp score over a 5 sceond time span and in 2 second steps.
        //    int span = (int)(secondsSpan * framesPerSecond);
        //    int step = (int)(2 * framesPerSecond);
        //    for (int r = 0; r < length - span; r++)
        //    {
        //        double[] subarray = DataTools.Subarray(chirps, r, span); //extract segment
        //        double[] peakValues = DataTools.GetPeakValues(subarray);
        //        int peakCount = peakValues.Count(a => (a > 0.0));
        //        if (peakCount > 8) peakCount = 8;  //up to a maximum of 8 peaks

        //        double sum = 0.0;
        //        for (int i = 0; i < peakCount; i++) //for each of the peaks
        //        {
        //            int maxIndex = DataTools.GetMaxIndex(peakValues);
        //            sum += peakValues[maxIndex];
        //            peakValues[maxIndex] = 0.0; //remove this peak from further consideration
        //        }
        //        sum /= (double)secondsSpan;     // get average score per second
        //        //sum /= (double)peakCount;     // get average score per peak

        //        if (sum > 1.0) sum = 1.0;

        //        for (int j = 0; j < span; j++)
        //        {
        //            if (sum > chirpScores[r + j]) chirpScores[r + j] = sum;
        //        }
        //        r += step;
        //    }

        //    return chirpScores;
        //}

        public static double[] ConvertChirpsToScoreArray(double[] chirps, double[] dBArray, double framesPerSecond)
        {
            int length = chirps.Length;
            double[] chirpScores = new double[length];
            int secondsSpan = 5; //calculate the chirp score over a 5 sceond time span and in 2 second steps.
            int span = (int)(secondsSpan * framesPerSecond);
            int step = (int)(2 * framesPerSecond);
            for (int r = 0; r < length - span; r++)
            {
                double[] subarray = DataTools.Subarray(chirps, r, span); //extract segment
                //double[] peakValues = DataTools.GetPeakValues(subarray);
                int peakCount = subarray.Count(a => (a > 0.0));
                if (peakCount > 8) peakCount = 8;  //up to a maximum of 8 peaks

                double sum = 0.0;
                for (int i = 0; i < peakCount; i++) //for each of the peaks
                {
                    int maxIndex = DataTools.GetMaxIndex(subarray);
                    sum += subarray[maxIndex];
                    subarray[maxIndex] = 0.0; //remove this peak from further consideration
                }
                sum /= (double)secondsSpan;     // get average score per second
                //sum /= (double)peakCount;     // get average score per peak

                if (sum > 1.0) sum = 1.0;

                for (int j = 0; j < span; j++)
                {
                    if (sum > chirpScores[r + j]) chirpScores[r + j] = sum;
                }
                r += step;
            }

            return chirpScores;
        }

        /// Checks acoustic activity that spills outside the kiwi bandwidth.
        /// use the periodicity array to cut down comoputaiton.
        /// Only look where we already know there is periodicity.
        public static double[] CalculateKiwiBandWidthScore(BaseSonogram sonogram, int minHz, int maxHz, double[] peakPeriodicity)
        {
            int frameCount = sonogram.FrameCount;
            double sonogramDuration = sonogram.FrameStep * frameCount;
            var scores = new double[frameCount];
            int secondsSpan = 10;
            TimeSpan span = new TimeSpan(0, 0, secondsSpan);
            int frameSpan = (int)Math.Round(secondsSpan * sonogram.FramesPerSecond);
            for (int r = 1; r < frameCount - frameSpan; r++)
            {
                if (peakPeriodicity[r] == 0.0) continue;
                TimeSpan start = new TimeSpan(0, 0, (int)(r / sonogram.FramesPerSecond));
                TimeSpan end = start + span;
                double score = CalculateKiwiBandWidthScore(sonogram, start, end, minHz, maxHz);
                for (int i = 0; i < frameSpan; i++)
                    if (scores[r + i] < score) scores[r + i] = score;
            }
            return scores;
        }
        /// <summary>
        /// Checks acoustic activity that spills outside the kiwi bandwidth.
        /// </summary>
        /// <returns></returns>
        public static double CalculateKiwiBandWidthScore(BaseSonogram sonogram, TimeSpan start, TimeSpan end, int minHz, int maxHz)
        {
            double[,] m = sonogram.Data;

            //set the time dimension
            int startFrame = (int)Math.Round(start.TotalSeconds * sonogram.FramesPerSecond);
            int endFrame = (int)Math.Round(end.TotalSeconds * sonogram.FramesPerSecond);
            //if (endFrame >= m.GetLength(1)) return 0.0;  //end of spectrum
            //int span = (int)Math.Round((end - start).TotalSeconds * sonogram.FramesPerSecond);
            int span = endFrame - startFrame + 1;

            //set the frequency dimension
            int minBin = (int)Math.Round(minHz / sonogram.FBinWidth);
            int maxBin = (int)Math.Round(maxHz / sonogram.FBinWidth);
            //int bandHt = (int)Math.Round((maxHz - minHz) / sonogram.FBinWidth);
            int bandHt = maxBin - minBin + 1;
            int halfHt = bandHt / 2;
            int hzBuffer = 150;
            int buffer = (int)Math.Round(hzBuffer / sonogram.FBinWidth); //avoid this margin around the main band

            //init the activity arrays
            double[] band_dB = new double[span]; //dB profile for kiwi band
            double[] upper_dB = new double[span]; //dB profile for band above kiwi band
            double[] lower_dB = new double[span]; //dB profile for band below kiwi band

            //get acoustic activity within the kiwi bandwidth and above it.
            for (int r = 0; r < span; r++)
            {
                for (int c = 0; c < bandHt; c++) band_dB[r] += m[startFrame + r, minBin + c]; //event dB profile
                for (int c = 0; c < halfHt; c++) upper_dB[r] += m[startFrame + r, maxBin + c + buffer];
                for (int c = 0; c < halfHt; c++) lower_dB[r] += m[startFrame + r, minBin - halfHt - buffer + c];
            }
            for (int r = 0; r < span; r++) band_dB[r] /= bandHt; //calculate averagesS.
            for (int r = 0; r < span; r++) upper_dB[r] /= halfHt;
            for (int r = 0; r < span; r++) lower_dB[r] /= halfHt;

            double upperCC = AutoAndCrossCorrelation.CorrelationCoefficient(band_dB, upper_dB);
            double lowerCC = AutoAndCrossCorrelation.CorrelationCoefficient(band_dB, lower_dB);
            if (upperCC < 0.0) upperCC = 0.0;
            if (lowerCC < 0.0) lowerCC = 0.0;
            double CCscore = upperCC + lowerCC;
            if (CCscore > 1.0) CCscore = 1.0;
            return 1 - CCscore;
        }

        public static List<AcousticEvent> ConvertScoreArray2Events(
            double[] dBarray,
            double[] intensity,
            double[] gridScore,
            double[] deltaPeriodScore,
            double[] chirpScores,
            double[] comboScore,
            double[] bwScore,
            int minHz,
            int maxHz,
            double framesPerSec,
            double freqBinWidth,
            double scoreThreshold,
            double minDuration,
            double maxDuration,
            TimeSpan segmentStartOffset)
        {
            int count = comboScore.Length;
            var events = new List<AcousticEvent>();
            //double maxPossibleScore = 5 * scoreThreshold; // used to calculate a normalised score bewteen 0 - 1.0
            bool isHit = false;
            double frameOffset = 1 / framesPerSec; // frame offset in fractions of second
            double startTime = 0.0;
            int startFrame = 0;

            // pass over all frames
            for (int i = 0; i < count; i++)
            {
                if ((isHit == false) && (comboScore[i] >= scoreThreshold))//start of an event
                {
                    isHit = true;
                    startTime = i * frameOffset;
                    startFrame = i;
                }
                else  // check for the end of an event
                    if ((isHit == true) && (comboScore[i] <= scoreThreshold)) // this is end of an event, so initialise it
                    {
                        isHit = false;

                        double endTime = i * frameOffset;
                        double duration = endTime - startTime;
                        if ((duration < minDuration) || (duration > maxDuration)) continue; //skip events with duration outside defined limits
                        AcousticEvent ev = new AcousticEvent(segmentStartOffset, startTime, duration, minHz, maxHz);
                        ev.SetTimeAndFreqScales(framesPerSec, freqBinWidth); //need time scale for later cropping of events

                        //
                        ev.kiwi_snrScore = CalculatePeakSnrScore(ev, dBarray);
                        ev.kiwi_intensityScore = LSKiwiHelper.CalculateAverageEventScore(ev, intensity);
                        ev.kiwi_gridScore = LSKiwiHelper.CalculateAverageEventScore(ev, gridScore);
                        ev.kiwi_deltaPeriodScore = LSKiwiHelper.CalculateAverageEventScore(ev, deltaPeriodScore);
                        ev.kiwi_chirpScore = LSKiwiHelper.CalculateAverageEventScore(ev, chirpScores);
                        ev.kiwi_bandWidthScore = LSKiwiHelper.CalculateAverageEventScore(ev, bwScore);
                        ev.kiwi_comboScore = LSKiwiHelper.CalculateAverageEventScore(ev, comboScore);
                        ev.Score = ev.kiwi_comboScore;                     //assume score already nomalised
                        ev.ScoreNormalised = ev.Score * ev.kiwi_bandWidthScore;      //discount the normalised score by the bandwidth score.

                        if (ev.ScoreNormalised > 1.0) ev.ScoreNormalised = 1.0;  // ... but just in case!

                        events.Add(ev);
                    }
            } //end of pass over all frames
            return events;
        }//end method ConvertScoreArray2Events()

        //this method assumes that the array has had backgroundnoise removed
        public static double CalculatePeakSnrScore(AcousticEvent ev, double[] dBarray)
        {
            int start = ev.Oblong.RowTop;
            int end = ev.Oblong.RowBottom;
            if (end > dBarray.Length) end = dBarray.Length - 1;
            int length = end - start + 1;

            double[] subarray = DataTools.Subarray(dBarray, start, length);
            double[] peakValues = DataTools.GetPeakValues(subarray);
            int peakCount = peakValues.Count(a => (a > 0.0));
            if (peakCount > 10) peakCount = 10;  //up to a maximum of 10 peaks

            double sum = 0.0;
            for (int i = 0; i < peakCount; i++)
            {
                int maxIndex = DataTools.GetMaxIndex(peakValues);
                sum += subarray[maxIndex];
                subarray[maxIndex] = 0.0; //remove this peak from further consideration
            }
            return sum / (double)peakCount;
        }

        //assume that the events are all from the same sex.
        public static void MergeEvents(List<AcousticEvent> events, double minGapInSeconds)
        {
            int count = events.Count;
            for (int i = count - 2; i >= 0; i--)
            {
                double endEv1 = events[i].TimeEnd;
                double startEv2 = events[i + 1].TimeStart;
                //string name1    = events[i].Name;
                //string name2    = events[i+1].Name;
                if ((startEv2 - endEv1) < 10) /*&& (name1 == name2))*/ // events are close so MergeEvents them
                {
                    events[i].Oblong = null;
                    events[i].TimeEnd = events[i + 1].TimeEnd;
                    if (events[i + 1].kiwi_intensityScore > events[i].kiwi_intensityScore) events[i].kiwi_intensityScore = events[i + 1].kiwi_intensityScore;
                    if (events[i + 1].kiwi_chirpScore > events[i].kiwi_chirpScore) events[i].kiwi_chirpScore = events[i + 1].kiwi_chirpScore;
                    if (events[i + 1].kiwi_deltaPeriodScore > events[i].kiwi_deltaPeriodScore) events[i].kiwi_deltaPeriodScore = events[i + 1].kiwi_deltaPeriodScore;
                    if (events[i + 1].kiwi_gridScore > events[i].kiwi_gridScore) events[i].kiwi_gridScore = events[i + 1].kiwi_gridScore;
                    if (events[i + 1].kiwi_snrScore > events[i].kiwi_snrScore) events[i].kiwi_snrScore = events[i + 1].kiwi_snrScore;
                    if (events[i + 1].kiwi_bandWidthScore > events[i].kiwi_bandWidthScore) events[i].kiwi_bandWidthScore = events[i + 1].kiwi_bandWidthScore;
                    events.Remove(events[i + 1]);
                    if (events[i].EventDurationSeconds > 80.0)
                    {
                        events.Remove(events[i]);
                        i--;
                    }
                }
            }
        } //MergeEvents()

        public static void CropEvents(List<AcousticEvent> events, double[] activity, double minDurationInSeconds)
        {
            double croppingSeverity = 0.2;
            int length = activity.Length;

            foreach (AcousticEvent ev in events)
            {
                int start = (int)(ev.TimeStart * ev.FramesPerSecond);
                int end = (int)(ev.TimeEnd * ev.FramesPerSecond);
                //int start = ev.oblong.RowTop;
                //int end = ev.oblong.RowBottom;
                double[] subArray = DataTools.Subarray(activity, start, end - start + 1);
                int[] bounds = DataTools.Peaks_CropLowAmplitude(subArray, croppingSeverity);

                int newMinRow = start + bounds[0];
                int newMaxRow = start + bounds[1];
                if (newMaxRow >= length) newMaxRow = length - 1;

                ev.Oblong = null;
                ev.TimeStart = newMinRow * ev.FrameOffset;
                ev.TimeEnd = newMaxRow * ev.FrameOffset;

                //int frameCount = (int)Math.Round(ev.Duration / ev.FrameOffset);
            }
            for (int i = events.Count - 1; i >= 0; i--) if (events[i].EventDurationSeconds < minDurationInSeconds) events.Remove(events[i]);
        } //CropEvents()

        static Image DrawSonogram(BaseSonogram sonogram, double[,] hits, List<Plot> scores, List<AcousticEvent> predictedEvents, double eventThreshold)
        {
            bool doHighlightSubband = false; bool add1kHzLines = true;
            Image_MultiTrack image = new Image_MultiTrack(sonogram.GetImage(doHighlightSubband, add1kHzLines));
            image.AddTrack(ImageTrack.GetTimeTrack(sonogram.Duration, sonogram.FramesPerSecond));
            if (scores != null)
            {
                foreach (Plot plot in scores)
                    image.AddTrack(ImageTrack.GetNamedScoreTrack(plot.data, 0.0, 1.0, plot.threshold, plot.title)); //assumes data normalised in 0,1
            }
            if (hits != null) image.OverlayRainbowTransparency(hits);

            if (predictedEvents.Count > 0)
            {
                foreach (AcousticEvent ev in predictedEvents) // set colour for the events
                {
                    ev.BorderColour = AcousticEvent.DefaultBorderColor;
                    ev.ScoreColour  = AcousticEvent.DefaultScoreColor;
                }
                image.AddEvents(predictedEvents, sonogram.NyquistFrequency, sonogram.Configuration.FreqBinCount, sonogram.FramesPerSecond);
            }
            return image.GetImage();
        } //DrawSonogram()

        public static DataTable WriteEvents2DataTable(List<AcousticEvent> predictedEvents)
        {
            if (predictedEvents == null) return null;
            string[] headers = { AnalysisKeys.EventCount,     //1
                                 AnalysisKeys.EventStartMin, //2
                                 AnalysisKeys.EventStartSec, //3
                                 AnalysisKeys.EventStartAbs, //4
                                 AnalysisKeys.KeySegmentDuration,//5
                                 AnalysisKeys.EventName,      //6
                                 AnalysisKeys.EventDuration,  //7
                                 AnalysisKeys.EventIntensity, //8
                                 LSKiwiHelper.key_GRID_SCORE,             //9
                                 LSKiwiHelper.key_DELTA_SCORE,            //10
                                 LSKiwiHelper.key_CHIRP_SCORE,            //11
                                 LSKiwiHelper.key_PEAKS_SNR_SCORE,        //12
                                 LSKiwiHelper.key_BANDWIDTH_SCORE,        //13
                                 LSKiwiHelper.key_COMBO_SCORE,            //14
                                 AnalysisKeys.EventNormscore,  //15
                               };
            //                   1                2               3              4                5              6               7              8
            Type[] types = { typeof(int), typeof(double), typeof(double), typeof(double), typeof(double), typeof(string), typeof(double), typeof(double),
            //                   9                10              11               12              13             14              15
                             typeof(double), typeof(double), typeof(double), typeof(double), typeof(double), typeof(double), typeof(double)  };

            var dataTable = DataTableTools.CreateTable(headers, types);
            if (predictedEvents.Count == 0) return dataTable;

            foreach (var ev in predictedEvents)
            {
                DataRow row = dataTable.NewRow();
                row[AnalysisKeys.EventStartAbs] = (double)ev.TimeStart;  //Set now - will overwrite later
                row[AnalysisKeys.EventStartSec] = (double)ev.TimeStart;  //EvStartSec
                row[AnalysisKeys.EventDuration] = (double)ev.EventDurationSeconds;   //duratio in seconds
                row[AnalysisKeys.EventName] = (string)ev.Name;       //
                row[AnalysisKeys.EventIntensity] = (double)ev.kiwi_intensityScore;  //
                row[LSKiwiHelper.key_BANDWIDTH_SCORE] = (double)ev.kiwi_bandWidthScore;
                row[LSKiwiHelper.key_DELTA_SCORE] = (double)ev.kiwi_deltaPeriodScore;
                row[LSKiwiHelper.key_GRID_SCORE] = (double)ev.kiwi_gridScore;
                row[LSKiwiHelper.key_CHIRP_SCORE] = (double)ev.kiwi_chirpScore;
                row[LSKiwiHelper.key_PEAKS_SNR_SCORE] = (double)ev.kiwi_snrScore;
                row[LSKiwiHelper.key_COMBO_SCORE] = (double)ev.kiwi_comboScore;
                row[AnalysisKeys.EventNormscore] = (double)ev.ScoreNormalised;
                dataTable.Rows.Add(row);
            }
            return dataTable;
        }

        /// <summary>
        /// Converts a DataTable of events to a datatable where one row = one minute of indices
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public DataTable ConvertEvents2Indices(DataTable dt, TimeSpan unitTime, TimeSpan sourceDuration, double scoreThreshold)
        {
            if (dt == null) return null;

            if ((sourceDuration == null) || (sourceDuration == TimeSpan.Zero)) return null;
            double units = sourceDuration.TotalSeconds / unitTime.TotalSeconds;
            int unitCount = (int)(units / 1);   //get whole minutes
            if (units % 1 > 0.0) unitCount += 1; //add fractional minute
            int[] eventsPerUnitTime = new int[unitCount]; //to store event counts
            int[] bigEvsPerUnitTime = new int[unitCount]; //to store counts of high scoring events

            foreach (DataRow ev in dt.Rows)
            {
                double eventStart = (double)ev[AnalysisKeys.EventStartAbs];
                double eventScore = (double)ev[AnalysisKeys.EventNormscore];
                int timeUnit = (int)(eventStart / unitTime.TotalSeconds);
                eventsPerUnitTime[timeUnit]++;
                if (eventScore > scoreThreshold) bigEvsPerUnitTime[timeUnit]++;
            }

            string[] headers = { AnalysisKeys.KeyStartMinute, AnalysisKeys.EventTotal, ("#Ev>" + scoreThreshold) };
            Type[] types = { typeof(int), typeof(int), typeof(int) };
            var newtable = DataTableTools.CreateTable(headers, types);

            for (int i = 0; i < eventsPerUnitTime.Length; i++)
            {
                int unitID = (int)(i * unitTime.TotalMinutes);
                newtable.Rows.Add(unitID, eventsPerUnitTime[i], bigEvsPerUnitTime[i]);
            }
            return newtable;
        }

        public static void AddContext2Table(DataTable dt, TimeSpan segmentStartMinute, TimeSpan recordingTimeSpan)
        {
            if (!dt.Columns.Contains(AnalysisKeys.KeySegmentDuration)) dt.Columns.Add(AnalysisKeys.KeySegmentDuration, typeof(double));
            if (!dt.Columns.Contains(AnalysisKeys.EventStartAbs)) dt.Columns.Add(AnalysisKeys.EventStartAbs, typeof(double));
            if (!dt.Columns.Contains(AnalysisKeys.EventStartMin)) dt.Columns.Add(AnalysisKeys.EventStartMin, typeof(double));
            double start = segmentStartMinute.TotalSeconds;
            foreach (DataRow row in dt.Rows)
            {
                row[AnalysisKeys.KeySegmentDuration] = recordingTimeSpan.TotalSeconds;
                row[AnalysisKeys.EventStartAbs] = start + (double)row[AnalysisKeys.EventStartSec];
                row[AnalysisKeys.EventStartMin] = start;
            }
        } //AddContext2Table()

        /// <summary>
        /// This method should no longer be used.
        /// It depends on use of the DataTable class which ceased when Anthony did a major refactor in mid-2014.
        /// </summary>
        /// <param name="fiCsvFile"></param>
        /// <param name="fiConfigFile"></param>
        /// <returns></returns>
        public Tuple<DataTable, DataTable> ProcessCsvFile(FileInfo fiCsvFile, FileInfo fiConfigFile)
        {
            //THIS METHOD HAS BEEn DEPRACATED
            //return DrawSummaryIndices.ProcessCsvFile(fiCsvFile, fiConfigFile);
            return null;
        }

        /// <summary>
        /// takes a data table of indices and normalises column values to values in [0,1].
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        public static DataTable NormaliseColumnsOfDataTable(DataTable dt)
        {
            string[] headers = DataTableTools.GetColumnNames(dt);
            string[] newHeaders = new string[headers.Length];

            List<double[]> newColumns = new List<double[]>();

            for (int i = 0; i < headers.Length; i++)
            {
                double[] values = DataTableTools.Column2ArrayOfDouble(dt, headers[i]); //get list of values
                if ((values == null) || (values.Length == 0)) continue;

                double min = 0;
                double max = 1;
                if (headers[i].Equals(AnalysisKeys.KeyAvSignalAmplitude))
                {
                    min = -50;
                    max = -5;
                    newColumns.Add(DataTools.NormaliseInZeroOne(values, min, max));
                    newHeaders[i] = headers[i] + "  (-50..-5dB)";
                }
                else //default is to NormaliseMatrixValues in [0,1]
                {
                    newColumns.Add(DataTools.normalise(values)); //NormaliseMatrixValues all values in [0,1]
                    newHeaders[i] = headers[i];
                }
            } //for loop

            //convert type int to type double due to normalisation
            Type[] types = new Type[newHeaders.Length];
            for (int i = 0; i < newHeaders.Length; i++) types[i] = typeof(double);
            var processedtable = DataTableTools.CreateTable(newHeaders, types, newColumns);
            return processedtable;
        }

        public AnalysisSettings DefaultSettings
        {
            get
            {
                return new AnalysisSettings
                {
                    AnalysisMaxSegmentDuration = TimeSpan.FromMinutes(1),
                    AnalysisMinSegmentDuration = TimeSpan.FromSeconds(30),
                    SegmentMediaType = MediaTypes.MediaTypeWav,
                    SegmentOverlapDuration = TimeSpan.Zero,
                    AnalysisTargetSampleRate = ResampleRate,
                };
            }
        }

        public string DefaultConfiguration
        {
            get
            {
                return string.Empty;
            }
        }

    }
}