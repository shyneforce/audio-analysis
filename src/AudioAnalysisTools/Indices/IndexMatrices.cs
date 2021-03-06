// --------------------------------------------------------------------------------------------------------------------
// <copyright file="IndexMatrices.cs" company="QutEcoacoustics">
// All code in this file and all associated files are the copyright and property of the QUT Ecoacoustics Research Group (formerly MQUTeR, and formerly QUT Bioacoustics Research Group).
// </copyright>
// <summary>
//   Defines the IndexMatrices type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace AudioAnalysisTools.Indices
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using Acoustics.Shared;
    using Acoustics.Shared.Contracts;
    using Acoustics.Shared.Csv;
    using AudioAnalysisTools.DSP;
    using AudioAnalysisTools.StandardSpectrograms;
    using log4net;
    using MoreLinq;
    using TowseyLibrary;

    public static class IndexMatrices
    {
        public const string MissingRowString = "<missing row>";
        private static readonly ILog Log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        /// <summary>
        /// All the passed files will be concatenated. Filtering needs to be done somewhere else.
        /// </summary>
        /// <param name="files">array of file names.</param>
        /// <param name="indexCalcDuration">used to match rows of indices to elapsed time in file names.</param>
        public static List<SummaryIndexValues> ConcatenateSummaryIndexFilesWithTimeCheck(FileInfo[] files, TimeSpan indexCalcDuration)
        {
            TimeSpan? offsetHint = new TimeSpan(10, 0, 0);
            DateTimeOffset[] dtoArray = new DateTimeOffset[files.Length];
            var summaryIndices = new List<SummaryIndexValues>();

            // accumulate the start times for each of the files
            for (int f = 0; f < files.Length; f++)
            {
                if (!files[f].Exists)
                {
                    LoggedConsole.WriteWarnLine($"WARNING: Concatenation Time Check: MISSING FILE: {files[f].FullName}");
                    continue;
                }

                if (!FileDateHelpers.FileNameContainsDateTime(files[f].Name, out var date, offsetHint))
                {
                    LoggedConsole.WriteWarnLine($"WARNING: Concatenation Time Check: INVALID DateTime in File Name {files[f].Name}");
                }

                dtoArray[f] = date;
            }

            // we use the fileName field to distinguish unique input source files
            // this Set allows us to check they are unique and render joins
            var sourceFileNames = new HashSet<string>();

            // now loop through the files again to extract the indices
            for (int i = 0; i < files.Length; i++)
            {
                if (!files[i].Exists)
                {
                    continue;
                }

                var rowsOfCsvFile = Csv.ReadFromCsv<SummaryIndexValues>(files[i], throwOnMissingField: false);

                // check all rows have fileName set
                var thisSourceFileNames = new HashSet<string>();
                foreach (var summaryIndexValues in rowsOfCsvFile)
                {
                    if (summaryIndexValues.FileName.IsNullOrEmpty())
                    {
                        throw new InvalidOperationException($"A supplied summary index file did not have the `{nameof(SummaryIndexValues.FileName)}` field populated. File: {files[i].FullName}");
                    }

                    thisSourceFileNames.Add(summaryIndexValues.FileName);
                }

                // check all found filenames are unique
                foreach (var sourceFileName in thisSourceFileNames)
                {
                    if (sourceFileNames.Contains(sourceFileName))
                    {
                        throw new InvalidOperationException(
                            $"The summary index files already read previously contained the filename {sourceFileName} - duplicates are not allowed. File: {files[i].FullName}");
                    }

                    sourceFileNames.Add(sourceFileName);
                }

                summaryIndices.AddRange(rowsOfCsvFile);

                // track the row counts
                int partialRowCount = rowsOfCsvFile.Count();

                // calculate elapsed time from the rows
                int accumulatedRowMinutes = (int)Math.Round(partialRowCount * indexCalcDuration.TotalMinutes);

                // calculate the partial elapsed minutes as indexed by file names.
                var elapsedMinutesInFileNames = 0;
                if (i < files.Length - 1)
                {
                    TimeSpan elapsedTimeAccordingtoFileNames = dtoArray[i + 1] - dtoArray[i];
                    elapsedMinutesInFileNames = (int)Math.Round(elapsedTimeAccordingtoFileNames.TotalMinutes);
                }
                else
                {
                    elapsedMinutesInFileNames = accumulatedRowMinutes; // a hack for the last file
                }

                // Check for Mismatch error in concatenation.
                if (accumulatedRowMinutes != elapsedMinutesInFileNames)
                {
                    string str1 = $"Concatenation: Elapsed Time Mismatch ERROR in csvFile {i + 1}/{files.Length}: {accumulatedRowMinutes} accumulatedRowMinutes != {elapsedMinutesInFileNames} elapsedMinutesInFileNames";
                    LoggedConsole.WriteWarnLine(str1);

                    //dictionary = RepairDictionaryOfArrays(dictionary, rowCounts[i], partialMinutes);
                    int scalingfactor = (int)Math.Round(60.0 / indexCalcDuration.TotalSeconds);
                    int minutesToAdd = elapsedMinutesInFileNames - accumulatedRowMinutes;
                    int rowsToAdd = minutesToAdd * scalingfactor;

                    // add in the missing summary index rows
                    for (int j = 0; j < rowsToAdd; j++)
                    {
                        var vector = new SummaryIndexValues { FileName = MissingRowString };
                        summaryIndices.Add(vector);
                    }
                }
            }

            // Can prune the list of summary indices as required.
            //int expectedRowCount = (int)Math.Round(numberOfMinutesInDay / indexCalcDuration.TotalMinutes);
            //if (totalRowCount != expectedRowCount)
            //{
            //    if (IndexMatrices.Verbose)
            //        LoggedConsole.WriteLine("WARNING: INCONSISTENT ELAPSED TIME CHECK from IndexMatrices.GetSummaryIndexFilesAndConcatenateWithTimeCheck() ");
            //    string str = String.Format("   Final Data Row Count = {0}     Estimated Cumulative Duration = {1} minutes", totalRowCount, expectedRowCount);
            //    if (IndexMatrices.Verbose)
            //        LoggedConsole.WriteLine(str);
            //    dictionary = RepairDictionaryOfArrays(dictionary, totalRowCount, expectedRowCount);
            //}

            return summaryIndices;
        }

        public static Dictionary<string, double[,]> GetSpectralIndexFilesAndConcatenate(
            DirectoryInfo[] dirs,
            string analysisType,
            string[] keys,
            IndexGenerationData indexGenerationData,
            bool verbose = false)
        {
            TimeSpan indexCalcTimeSpan = indexGenerationData.IndexCalculationDuration;
            Dictionary<string, double[,]> spectrogramMatrices = new Dictionary<string, double[,]>();

            foreach (string key in keys)
            {
                //DateTime now1 = DateTime.Now;
                string pattern = "*__" + analysisType + "." + key + ".csv";
                var files = GetFilesInDirectories(dirs, pattern);

                if (files.Length == 0)
                {
                    LoggedConsole.WriteWarnLine($"{key} WARNING: No csv files found for KEY=" + key);
                    continue;
                }

                List<double[,]> matrices = ConcatenateSpectralIndexFilesWithTimeCheck(files, indexCalcTimeSpan, key);
                double[,] m = MatrixTools.ConcatenateMatrixRows(matrices);

                //Dictionary<string, double[,]> dict = spectralIndexValues.ToTwoDimensionalArray(SpectralIndexValues.CachedSelectors, TwoDimensionalArray.Rotate90ClockWise);

                m = MatrixTools.MatrixRotate90Anticlockwise(m);
                spectrogramMatrices.Add(key, m);

                //var now2 = DateTime.Now;
                //var et = now2 - now1;
                //if (verbose)
                //{
                //    LoggedConsole.WriteLine($"\t\tTime to read <{key}> spectral index files = {et.TotalSeconds:f2} seconds");
                //}
            }

            return spectrogramMatrices;
        }

        /// <summary>
        /// Concatenates a series of Spectral Index files with a time check,
        ///  i.e. check elapse time in file names against accumulated rows of indices.
        /// </summary>
        /// <param name="files">All the passed files will be concatenated. Filtering needs to be done somewhere else.</param>
        /// <param name="indexCalcDuration">used to match rows of indices to elapsed time in file names.</param>
        /// <param name="key">this is used only in case need to write an error message. It identifies the key.</param>
        public static List<double[,]> ConcatenateSpectralIndexFilesWithTimeCheck(FileInfo[] files, TimeSpan indexCalcDuration, string key)
        {
            TimeSpan? offsetHint = new TimeSpan(10, 0, 0);
            var datesAndFiles = new (DateTimeOffset Date, FileInfo File)[files.Length];
            var matrices = new List<double[,]>();

            // accumulate the start times for each of the files
            for (int f = 0; f < files.Length; f++)
            {
                var file = files[f];
                if (!file.Exists)
                {
                    LoggedConsole.WriteWarnLine($"WARNING: {key} Concatenation Time Check: MISSING FILE: {files[f].FullName}");
                    continue;
                }

                if (!FileDateHelpers.FileNameContainsDateTime(file.Name, out var startDto, offsetHint))
                {
                    LoggedConsole.WriteWarnLine($"WARNING: {key} Concatenation Time Check: INVALID DateTime in File Name {file.Name}");
                }

                datesAndFiles[f] = (startDto, file);
            }

            // list of file needs to be sorted (relying on system sorting is not reliable)
            datesAndFiles = datesAndFiles.OrderBy(df => df.Date).ToArray();

            string fileName = datesAndFiles[0].File.Name;
            string fileExt = fileName.Substring(fileName.Length - 7);

            // now loop through the files again to extract the indices
            for (int i = 0; i < datesAndFiles.Length; i++)
            {
                var file = datesAndFiles[i].File;
                if (!file.Exists)
                {
                    continue;
                }

                var matrix = Csv.ReadMatrixFromCsv<double>(file, TwoDimensionalArray.None);
                matrices.Add(matrix);

                // track the row counts
                int partialRowCount = matrix.GetLength(0);

                // calculate elapsed time from the rows
                int accumulatedRowMinutes = (int)Math.Round(partialRowCount * indexCalcDuration.TotalMinutes);

                //track the elapsed minutes
                // calculate the partial elapsed time indexed by file names.
                var elapsedMinutesInFileNames = 0;
                var length = datesAndFiles.Length;
                if (i < length - 1)
                {
                    TimeSpan partialElapsedTime = datesAndFiles[i + 1].Date - datesAndFiles[i].Date;
                    elapsedMinutesInFileNames = (int)Math.Round(partialElapsedTime.TotalMinutes);
                }
                else
                {
                    elapsedMinutesInFileNames = accumulatedRowMinutes; // a hack for the last file
                }

                if (accumulatedRowMinutes < elapsedMinutesInFileNames)
                {
                    string str1 = $"{key} Concatenation: Elapsed Time Mismatch ERROR in csvFile {i + 1}/{files.Length}: {accumulatedRowMinutes} accumulatedRowMinutes != {elapsedMinutesInFileNames} elapsedMinutesInFileNames";
                    LoggedConsole.WriteWarnLine(str1);

                    int scalingFactor = (int)Math.Round(60.0 / indexCalcDuration.TotalSeconds);
                    int minutesToAdd = elapsedMinutesInFileNames - accumulatedRowMinutes;
                    int rows2Add = minutesToAdd * scalingFactor;

                    int columnCount = matrices[0].GetLength(1);
                    var emptyMatrix = new double[rows2Add, columnCount];
                    if (fileExt.StartsWith("BGN"))
                    {
                        for (int r = 0; r < rows2Add; r++)
                        {
                            for (int c = 0; c < columnCount; c++)
                            {
                                // initialise with zero signal decibel value
                                emptyMatrix[r, c] = SNR.MinimumDbBoundForZeroSignal;

                                // OR initialise with low decibel value for environmental noise
                                //emptyMatrix[r, c] = SNR.MinimumDbBoundForEnvironmentalNoise;
                            }
                        }
                    }

                    matrices.Add(emptyMatrix);
                }
            }

            // Can prune the list of summary indices as required.
            //int expectedRowCount = (int)Math.Round(numberOfMinutesInDay / indexCalcDuration.TotalMinutes);
            //if (totalRowCount != expectedRowCount)
            //{
            //    if (IndexMatrices.Verbose)
            //        LoggedConsole.WriteLine("WARNING: INCONSISTENT ELAPSED TIME CHECK from IndexMatrices.GetSummaryIndexFilesAndConcatenateWithTimeCheck() ");
            //    string str = String.Format("   Final Data Row Count = {0}     Estimated Cumulative Duration = {1} minutes", totalRowCount, expectedRowCount);
            //    if (IndexMatrices.Verbose)
            //        LoggedConsole.WriteLine(str);
            //    dictionary = RepairDictionaryOfArrays(dictionary, totalRowCount, expectedRowCount);
            //}

            return matrices;
        }

        public static FileInfo[] GetFilesInDirectory(string path, string pattern)
        {
            var dirInfo = new DirectoryInfo(path);
            if (!dirInfo.Exists)
            {
                var directoryNotFoundException = new DirectoryNotFoundException(path);
                LoggedConsole.WriteFatalLine("DIRECTORY DOES NOT EXIST", directoryNotFoundException);
                throw directoryNotFoundException;
            }

            FileInfo[] files = dirInfo.GetFiles(pattern, SearchOption.AllDirectories);
            if (files.Length == 0)
            {
                LoggedConsole.WriteErrorLine($"No file names match pattern <{pattern}>. Returns empty list of files");
            }

            Array.Sort(files, (f1, f2) => f1.Name.CompareTo(f2.Name));

            return files;
        }

        /// <summary>
        /// Returns a unique, sorted, list of file paths, sorted on file name.
        /// IMPORTANT: Sorts on alphanumerics, NOT on date or time encoded in the file name.
        /// </summary>
        public static FileInfo[] GetFilesInDirectories(DirectoryInfo[] directories, string pattern)
        {
            var fileList = new List<FileInfo>();

            foreach (var dir in directories)
            {
                if (!dir.Exists)
                {
                    var directoryNotFoundException = new DirectoryNotFoundException(dir.FullName);
                    LoggedConsole.WriteFatalLine("DIRECTORY DOES NOT EXIST", directoryNotFoundException);
                    throw directoryNotFoundException;
                }

                //FileInfo[] files = dir.GetFiles(pattern, SearchOption.TopDirectoryOnly);
                var files = dir.GetFiles(pattern, SearchOption.AllDirectories);
                fileList.AddRange(files);
            }

            FileInfo[] returnList = fileList.DistinctBy(x => x.FullName).OrderBy(x => x.Name).ToArray();

            return returnList;
        }

        public static Dictionary<string, double[,]> AddDerivedIndices(Dictionary<string, double[,]> spectrogramMatrices)
        {
            string key = "PMN";
            string newKey = "Sqrt" + key;
            if (spectrogramMatrices.ContainsKey(key) && !spectrogramMatrices.ContainsKey(newKey))
            {
                // add another matrix with square root and log transform  of values for lop-sided distributions
                var m = spectrogramMatrices[key];
                spectrogramMatrices.Add(newKey, MatrixTools.SquareRootOfValues(m));
                newKey = "Log" + key;
                spectrogramMatrices.Add(newKey, MatrixTools.LogTransform(m));
            }

            // add another matrix with square root and log transform of values for lop-sided distributions
            key = "ENT";
            newKey = "Sqrt" + key;
            if (spectrogramMatrices.ContainsKey(key) && !spectrogramMatrices.ContainsKey(newKey))
            {
                var m = spectrogramMatrices[key];
                spectrogramMatrices.Add(newKey, MatrixTools.SquareRootOfValues(m));
            }

            newKey = "Log" + key;
            if (spectrogramMatrices.ContainsKey(key) && !spectrogramMatrices.ContainsKey(newKey))
            {
                var m = spectrogramMatrices[key];
                spectrogramMatrices.Add(newKey, MatrixTools.LogTransform(m));
            }

            return spectrogramMatrices;
        }

        /// <summary>
        /// DO NOT DELETE THIS METHOD DESPITE NO REFERENCES
        /// It can be useful in future.
        /// </summary>
        public static Dictionary<string, double[]> AddDerivedIndices(Dictionary<string, double[]> summaryIndices)
        {
            // insert some transformed data columns
            summaryIndices.Add("SqrtTempEntropy", DataTools.SquareRootOfValues(summaryIndices["TemporalEntropy"]));

            // insert some transformed data columns
            var epsilon = Acoustics.Tools.Wav.WavReader.CalculateEpsilonForRescaledInteger(16);
            summaryIndices.Add("LogTempEntropy", DataTools.Log10Values(summaryIndices["TemporalEntropy"], epsilon));

            // Calculate Normalised Difference Soundscape Index if not already done
            // caluclate two ratios for three bands.  DO NOT CHANGE THESE KEYS
            string ndsiKey = "NDSI-LM";
            if (!summaryIndices.ContainsKey(ndsiKey))
            {
                summaryIndices = AddNDSI_GageGauge(summaryIndices, ndsiKey);
            }

            ndsiKey = "NDSI-MH";
            if (!summaryIndices.ContainsKey(ndsiKey))
            {
                summaryIndices = AddNDSI_GageGauge(summaryIndices, ndsiKey);
            }

            return summaryIndices;
        }

        /// <summary>
        /// This method reads spectrogram csv files where the first row contains column names
        /// and the first column contains row/time names.
        /// Note: no rotation of data is done!.
        /// </summary>
        public static double[,] ReadSpectrogram(FileInfo csvFile, out int binCount, TwoDimensionalArray transform = TwoDimensionalArray.None)
        {
            double[,] matrix = Csv.ReadMatrixFromCsv<double>(csvFile, transform);
            binCount = matrix.GetLength(1);
            return matrix;
        }

        /// <summary>
        /// Returns dictionary of spectral indices.
        /// Assumes both arrays of same length and keys correspond to file name.
        /// TODO: Do this better one day!.
        /// </summary>
        public static Dictionary<string, double[,]> ReadSummaryIndexFiles(FileInfo[] files, string[] keys)
        {
            int count = files.Length;
            var dict = new Dictionary<string, double[,]>();
            for (int c = 0; c < count; c++)
            {
                double[,] matrix = ReadSpectrogram(files[c], out var freqBinCount);
                dict.Add(keys[c], matrix);
            }

            return dict;
        }

        public static Dictionary<string, double[,]> ReadSpectralIndices(DirectoryInfo ipdir, string fileName, string analysisTag, string[] keys)
        {
            // parallel reading of CSV files
            var readData = keys.AsParallel()
                .Select(ReadInSingleCsvFile)
                .Where(x => x != null);

            // actual work done here
            var timer = Stopwatch.StartNew();

            // ReSharper disable once PossibleInvalidOperationException
            var spectrogramMatrices = readData.ToDictionary(kvp => kvp.Value.Item1, kvp => kvp.Value.Item2);

            // ReSharper restore PossibleInvalidOperationException
            timer.Stop();
            Log.Info($"Time to read spectral index files = {timer.Elapsed.TotalSeconds} seconds");

            if (spectrogramMatrices.Count == 0)
            {
                LoggedConsole.WriteWarnLine(
                    "WARNING: from method IndexMatrices.ReadSpectralIndices()\n\t\tNO FILES were read from this directory: "
                    + ipdir);
            }

            return spectrogramMatrices;

            (string, double[,])? ReadInSingleCsvFile(string indexKey)
            {
                Log.Info($"Starting to read CSV file for index {indexKey}");
                Stopwatch singleTimer = Stopwatch.StartNew();

                var file = ipdir.CombineFile(FilenameHelpers.AnalysisResultName(fileName, analysisTag + "." + indexKey, "csv"));
                double[,] matrix;
                if (file.Exists)
                {
                    matrix = ReadSpectrogram(file, out var freqBinCount, TwoDimensionalArray.Rotate90AntiClockWise);
                }
                else
                {
                    Log.Warn($"IndexMatrices.ReadSpectralIndices(): {indexKey} File does not exist: {file.FullName}");
                    return null;
                }

                singleTimer.Stop();
                Log.Debug($"Time to read spectral index file <{indexKey}> = {singleTimer.Elapsed.TotalSeconds} seconds");
                return (indexKey, matrix);
            }
        }

        /// <summary>
        /// Compresses the spectral index data in the temporal direction by a factor derived from the data scale and
        /// required image scale.
        /// In most cases, the compression is done by taking the average. ACI, ENT, BGN, and PMN are special cases,
        /// requiring a special form of averaging.
        /// This method got more complicated in June 2016 when it was refactored to cope with recording blocks less than
        /// one minute long.
        /// </summary>
        /// <param name="spectra">The spectra to compress as a dictionary of spectrogram matrices.</param>
        /// <param name="imageScale">The scale (time resolution) of the compressed output spectrogram.</param>
        /// <param name="dataScale">
        /// The scale (time resolution) of the input spectral indices. See <paramref name="spectra"/>.
        /// </param>
        /// <param name="roundingFunc">
        /// How fractional spectra should be dealt with.
        /// It should be one of or similar to <see cref="Math.Round(double)"/>,
        /// <see cref="Math.Floor(double)"/>, or <see cref="Math.Ceiling(double)"/>.
        /// </param>
        public static Dictionary<string, double[,]> CompressIndexSpectrograms(
            Dictionary<string, double[,]> spectra,
            TimeSpan imageScale,
            TimeSpan dataScale,
            Func<double, double> roundingFunc = null)
        {
            roundingFunc ??= Math.Floor;

            // the scaling factor should usually be > 1.0.
            var rawScalingFactor = imageScale.Ticks / (double)dataScale.Ticks;
            int scalingFactor = (int)Math.Round(rawScalingFactor);

            Contract.Requires(
                Math.Abs(scalingFactor - rawScalingFactor) < 0.0000001,
                "CompressIndexSpectrograms only supports rescaling between factors that produce integer ratios");

            var compressedSpectra = new Dictionary<string, double[,]>();

            // if there is no need to compress, simply return
            if (scalingFactor == 1)
            {
                return spectra;
            }

            //process each spectrogram in turn
            foreach (string key in spectra.Keys)
            {
                double[,] inputMatrix = spectra[key];
                int rowCount = inputMatrix.GetLength(0);
                int colCount = inputMatrix.GetLength(1);

                int compressionWindow = scalingFactor;
                int compressedLength = (int)roundingFunc((double)colCount / scalingFactor);
                if (compressedLength < 1)
                {
                    compressedLength = 1;
                }

                // init compressed output matrix
                var outputMatrix = new double[rowCount, compressedLength];

                // using this form to ensure we can reach the end of array
                int outputColCount = (compressedLength - 1) * scalingFactor;

                // the ENTROPY matrix requires separate calculation
                if (key == nameof(SpectralIndexValues.ENT))
                {
                    inputMatrix = spectra[nameof(SpectralIndexValues.SUM)];
                    for (int r = 0; r < rowCount; r++)
                    {
                        for (int c = 0; c <= outputColCount; c += scalingFactor)
                        {
                            // partial compression windows at the end of the array
                            var windowSize = Math.Min(colCount - c, compressionWindow);
                            double[] tempArray = new double[windowSize];
                            for (int i = c; i < c + windowSize; i++)
                            {
                                // square the amplitude to give energy
                                tempArray[i - c] = inputMatrix[r, i] * inputMatrix[r, i];
                            }

                            double entropy = DataTools.EntropyNormalised(tempArray);
                            if (double.IsNaN(entropy))
                            {
                                entropy = 1.0;
                            }

                            var colIndex = c / scalingFactor;
                            outputMatrix[r, colIndex] = 1 - entropy;
                        }
                    }
                }
                else if (key == nameof(SpectralIndexValues.PMN) || key == nameof(SpectralIndexValues.BGN))
                {
                    // indices whose units are in decibels require separate calculation
                    // i.e. PMN (POWER MINUS NOISE) and BGN (background noise)
                    inputMatrix = spectra[key];
                    for (int r = 0; r < rowCount; r++)
                    {
                        for (int c = 0; c <= outputColCount; c += scalingFactor)
                        {
                            // partial compression windows at the end of the array
                            var windowSize = Math.Min(colCount - c, compressionWindow);
                            double[] tempArray = new double[windowSize];
                            for (int i = c; i < c + windowSize; i++)
                            {
                                //store original values in temp array
                                tempArray[i - c] = inputMatrix[r, i];
                            }

                            var colIndex = c / scalingFactor;
                            outputMatrix[r, colIndex] = SpectrogramTools.AverageAnArrayOfDecibelValues(tempArray);
                        }
                    }
                }
                else if (key == nameof(SpectralIndexValues.ACI))
                {
                    // THE ACI matrix requires separate calculation
                    for (int r = 0; r < rowCount; r++)
                    {
                        for (int c = 0; c <= outputColCount; c += scalingFactor)
                        {
                            var difSum = 0.0;
                            var sumSum = 0.0;
                            for (int i = c; i < c + compressionWindow; i++)
                            {
                                // partial compression windows at the end of the array
                                if (i >= colCount)
                                {
                                    break;
                                }

                                difSum += spectra[nameof(SpectralIndexValues.DIF)][r, i];
                                sumSum += spectra[nameof(SpectralIndexValues.SUM)][r, i];
                            }

                            var colIndex = c / scalingFactor;
                            outputMatrix[r, colIndex] = difSum / sumSum;
                        }
                    }
                }
                else
                {
                    // average all other spectral indices
                    inputMatrix = spectra[key];
                    for (int r = 0; r < rowCount; r++)
                    {
                        for (int c = 0; c <= outputColCount; c += scalingFactor)
                        {
                            var sum = 0.0;
                            int count = 0;
                            for (int i = c; i < c + compressionWindow; i++)
                            {
                                // partial compression windows at the end of the array
                                if (i >= colCount)
                                {
                                    break;
                                }

                                count++;
                                sum += inputMatrix[r, i];
                            }

                            var colIndex = c / scalingFactor;
                            outputMatrix[r, colIndex] = sum / count;
                        }
                    }
                }

                compressedSpectra[key] = outputMatrix;
            }

            return compressedSpectra;
        }

        public static Dictionary<string, double[,]> ReadSpectrogramCsvFiles(DirectoryInfo ipdir, string fileName, string indexKeys, out int freqBinCount)
        {
            string[] keys = indexKeys.Split('-');
            return ReadSpectrogramCsvFiles(ipdir, fileName, keys, out freqBinCount);
        }

        /// <summary>
        /// Reads a list of Spectrogram Csv Files.
        /// </summary>
        /// <param name="ipdir">input dir.</param>
        /// <param name="fileName">the file name.</param>
        /// <param name="keys">an array of keys.</param>
        /// <param name="freqBinCount">number of freq bins.</param>
        public static Dictionary<string, double[,]> ReadSpectrogramCsvFiles(DirectoryInfo ipdir, string fileName, string[] keys, out int freqBinCount)
        {
            var dict = new Dictionary<string, double[,]>();
            string warning = null;
            freqBinCount = 256;
            for (int key = 0; key < keys.Length; key++)
            {
                var file = new FileInfo(Path.Combine(ipdir.FullName, fileName + "." + keys[key] + ".csv"));
                if (file.Exists)
                {
                    double[,] matrix = ReadSpectrogram(file, out var binCount);
                    matrix = MatrixTools.MatrixRotate90Anticlockwise(matrix);
                    dict.Add(keys[key], matrix);
                    freqBinCount = binCount;
                }
                else
                {
                    if (warning == null)
                    {
                        warning = "\nWARNING: from method IndexMatrices.ReadSpectrogramCsvFiles()";
                    }

                    warning += $"\n      {keys[key]} File does not exist: {file.FullName}";
                }
            }

            if (warning != null)
            {
                LoggedConsole.WriteLine(warning);
            }

            if (dict.Count != 0)
            {
                return dict;
            }

            LoggedConsole.WriteLine("WARNING: from method IndexMatrices.ReadSpectrogramCsvFiles()");
            LoggedConsole.WriteLine("         NO FILES were read from this directory: " + ipdir);

            return dict;
        }

        public static Dictionary<string, double[]> AddNDSI_GageGauge(Dictionary<string, double[]> dictionaryOfCsvColumns, string newKey)
        {
            const string highKey = "HighFreqCover";
            const string midKey = "MidFreqCover";
            const string lowKey = "LowFreqCover";
            if (newKey.EndsWith("-LM"))
            {
                if (!dictionaryOfCsvColumns.ContainsKey(midKey))
                {
                    return null;
                }

                if (!dictionaryOfCsvColumns.ContainsKey(lowKey))
                {
                    return null;
                }

                double[] midArray = dictionaryOfCsvColumns[midKey];
                double[] lowArray = dictionaryOfCsvColumns[lowKey];
                if (lowArray.Length != midArray.Length)
                {
                    return null;
                }

                var array = new double[lowArray.Length];
                for (int i = 0; i < lowArray.Length; i++)
                {
                    array[i] = (midArray[i] - lowArray[i]) / (midArray[i] + lowArray[i]);
                }

                dictionaryOfCsvColumns.Add(newKey, array);
            }
            else if (newKey.EndsWith("-MH"))
            {
                if (!dictionaryOfCsvColumns.ContainsKey(midKey))
                {
                    return null;
                }

                if (!dictionaryOfCsvColumns.ContainsKey(highKey))
                {
                    return null;
                }

                double[] midArray = dictionaryOfCsvColumns[midKey];
                double[] highArray = dictionaryOfCsvColumns[highKey];
                if (highArray.Length != midArray.Length)
                {
                    return null;
                }

                var array = new double[highArray.Length];
                for (int i = 0; i < highArray.Length; i++)
                {
                    array[i] = (highArray[i] - midArray[i]) / (highArray[i] + midArray[i]);
                }

                dictionaryOfCsvColumns.Add(newKey, array);
            }

            return dictionaryOfCsvColumns;
        }
    }
}