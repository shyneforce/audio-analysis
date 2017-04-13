﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SpectrogramConstants.cs" company="QutEcoacoustics">
// All code in this file and all associated files are the copyright and property of the QUT Ecoacoustics Research Group (formerly MQUTeR, and formerly QUT Bioacoustics Research Group).
// </copyright>
// <summary>
//   Defines the SpectrogramConstants type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace AudioAnalysisTools.LongDurationSpectrograms
{
    using System;

    using Acoustics.Shared;

    using AnalysisBase;

    public static class SpectrogramConstants
    {
        public const string DefaultAnalysisType = "Towsey.Acoustic";

        public const string RGBMap_DEFAULT     = "ACI-ENT-CVR"; //R-G-B

        // AVG was changed to POW in March 2015 because value was signal power and POW is more descriptive.
        public const string RGBMap_BGN_POW_CVR = "BGN-POW-CVR"; //R-G-B
        public const string RGBMap_BGN_POW_EVN = "BGN-POW-EVN"; //R-G-B
        public const string RGBMap_BGN_POW_SPT = "BGN-POW-SPT"; //R-G-B
        public const string RGBMap_BGN_POW_CLS = "BGN-POW-CLS"; //R-G-B

        public const string RGBMap_BGN_AVG_CVR = "BGN-AVG-CVR"; //R-G-B
        public const string RGBMap_BGN_AVG_EVN = "BGN-AVG-EVN"; //R-G-B
        public const string RGBMap_BGN_AVG_SPT = "BGN-AVG-SPT"; //R-G-B
        public const string RGBMap_BGN_AVG_CLS = "BGN-AVG-CLS"; //R-G-B

        public const string RGBMap_ACI_ENT_POW = "ACI-ENT-POW"; //R-G-B
        public const string RGBMap_ACI_ENT_CVR = "ACI-ENT-CVR"; //R-G-B
        public const string RGBMap_ACI_ENT_CLS = "ACI-ENT-CLS"; //R-G-B
        public const string RGBMap_ACI_ENT_EVN = "ACI-ENT-EVN"; //R-G-B
        public const string RGBMap_ACI_ENT_SPT = "ACI-ENT-SPT"; //R-G-B
        public const string RGBMap_ACI_CVR_ENT = "ACI-CVR-ENT";


        // these parameters manipulate the colour map and appearance of the false-colour LONG DURATION spectrogram
        public const double BACKGROUND_FILTER_COEFF = 0.75; //must be value <=1.0
        public const double COLOUR_GAIN = 2.0;

        // These parameters describe the time and frequency scales for drawing X and Y axes on LONG DURATION spectrograms
        public static TimeSpan X_AXIS_TIC_INTERVAL = TimeSpan.FromMinutes(60);  // default assumes one minute spectra and 60 spectra per hour
        public static TimeSpan MINUTE_OFFSET       = TimeSpan.Zero;    // assume recording starts at zero minute of day i.e. midnight
        public static int SAMPLE_RATE = AppConfigHelper.GetInt(AppConfigHelper.DefaultTargetSampleRateKey);  // default value - after resampling
        public const int FRAME_LENGTH = 512;    // default value - from which spectrogram was derived
        public const int HEIGHT_OF_TITLE_BAR = 24;
    }
}
