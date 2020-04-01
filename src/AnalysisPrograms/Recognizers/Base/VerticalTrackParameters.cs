// <copyright file="VerticalTrackParameters.cs" company="QutEcoacoustics">
// All code in this file and all associated files are the copyright and property of the QUT Ecoacoustics Research Group (formerly MQUTeR, and formerly QUT Bioacoustics Research Group).
// </copyright>

namespace AnalysisPrograms.Recognizers.Base
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Acoustics.Shared;
    using AudioAnalysisTools;
    using AudioAnalysisTools.StandardSpectrograms;

    /// <summary>
    /// Parameters needed from a config file to detect vertical track components i.e. events which are completed within very few time frames, i.e. whips and near clicks.
    /// </summary>
    [YamlTypeTag(typeof(VerticalTrackParameters))]
    public class VerticalTrackParameters : CommonParameters
    {
        /// <summary>
        /// Gets or sets the minimum bandwidth, units = Hertz.
        /// </summary>
        public int? MinBandwidthHertz { get; set; }

        /// <summary>
        /// Gets or sets maximum bandwidth, units = Hertz.
        /// </summary>
        public int? MaxBandwidthHertz { get; set; }

        /// <summary>
        /// EXPANATION: A vertical track is a near click or rapidly frequency-modulated tone. A good example is the whip component of the whip-bird call.
        /// They would typically be only a few time-frames duration.
        /// THis method averages dB log values incorrectly but it is faster than doing many log conversions and is accurate enough for the purpose.
        /// </summary>
        public static (List<AcousticEvent> Events, double[] Intensity) GetVerticalTracks(
            SpectrogramStandard sonogram,
            int minHz,
            int maxHz,
            int nyquist,
            double decibelThreshold,
            int minBandwidthHertz,
            int maxBandwidthHertz,
            TimeSpan segmentStartOffset)
        {
            var sonogramData = sonogram.Data;
            int frameCount = sonogramData.GetLength(0);
            int binCount = sonogramData.GetLength(1);

            double binWidth = nyquist / (double)binCount;
            int minBin = (int)Math.Round(minHz / binWidth);
            int maxBin = (int)Math.Round(maxHz / binWidth);
            int bandwidthBinCount = maxBin - minBin + 1;

            // list of accumulated acoustic events
            var events = new List<AcousticEvent>();
            var temporalIntensityArray = new double[frameCount];

            // for all time frames except 1st and last allowing for edge effects.
            for (int t = 1; t < frameCount - 1; t++)
            {
                // set up an intensity array for all frequency bins in this frame.
                double[] trackIntensity = new double[bandwidthBinCount];

                // for all frequency bins except top and bottom in this time frame
                for (int bin = minBin; bin < maxBin; bin++)
                {
                    // This is where the profile of a vertical ridge-track is defined
                    if (sonogramData[t, bin] < sonogramData[t - 1, bin] || sonogramData[t, bin] < sonogramData[t + 1, bin])
                    {
                        continue;
                    }

                    trackIntensity[bin - minBin] = sonogramData[t, bin];
                    //trackIntensity[bin - minBin] = sonogramData[t, bin] - sonogramData[t - 1, bin];
                    trackIntensity[bin - minBin] = Math.Max(0.0, trackIntensity[bin - minBin]);
                }

                if (trackIntensity.Max() < decibelThreshold)
                {
                    continue;
                }

                // Extract the events based on bandwidth and threshhold.
                var acousticEvents = ConvertSpectralArrayToVerticalTrackEvents(
                    trackIntensity,
                    minHz,
                    sonogram.FramesPerSecond,
                    sonogram.FBinWidth,
                    decibelThreshold,
                    minBandwidthHertz,
                    maxBandwidthHertz,
                    t,
                    segmentStartOffset);

                // add each event score to combined temporal intensity array
                foreach (var ae in acousticEvents)
                {
                    var avClickIntensity = ae.Score;
                    temporalIntensityArray[t] += avClickIntensity;
                }

                // add new events to list of events
                events.AddRange(acousticEvents);
            }

            // combine proximal events that occupy similar frequency band
            var startDifference = TimeSpan.FromSeconds(0.5);
            var hertzDifference = 500;
            events = AcousticEvent.CombineSimilarProximalEvents(events, startDifference, hertzDifference);

            // now combine overlapping events. THis will help in some cases to combine related events.
            // but can produce some spurious results.
            events = AcousticEvent.CombineOverlappingEvents(events, segmentStartOffset);

            return (events, temporalIntensityArray);
        }

        /// <summary>
        /// A general method to convert an array of score values to a list of AcousticEvents.
        /// NOTE: The score array is assumed to be a spectrum of dB intensity.
        /// The method uses the passed scoreThreshold in order to calculate a normalised score.
        /// Max possible score := threshold * 5.
        /// normalised score := score / maxPossibleScore.
        /// Some analysis techniques (e.g. Oscillation Detection) have their own methods for extracting events from score arrays.
        /// </summary>
        /// <param name="trackIntensityArray">the array of click intensity.</param>
        /// <param name="minHz">lower freq bound of the search band for click events.</param>
        /// <param name="framesPerSec">the time scale required by AcousticEvent class.</param>
        /// <param name="freqBinWidth">the freq scale required by AcousticEvent class.</param>
        /// <param name="scoreThreshold">threshold for the intensity values.</param>
        /// <param name="minBandwidth">bandwidth of click must exceed this to qualify as an event.</param>
        /// <param name="maxBandwidth">bandwidth of click must be less than this to qualify as an event.</param>
        /// <param name="frameNumber">time of start of the current frame.</param>
        /// <returns>a list of acoustic events.</returns>
        public static List<AcousticEvent> ConvertSpectralArrayToVerticalTrackEvents(
            double[] trackIntensityArray,
            int minHz,
            double framesPerSec,
            double freqBinWidth,
            double scoreThreshold,
            int minBandwidth,
            int maxBandwidth,
            int frameNumber,
            TimeSpan segmentStartOffset)
        {
            int binCount = trackIntensityArray.Length;
            var events = new List<AcousticEvent>();
            double maxPossibleScore = 5 * scoreThreshold; // used to calculate a normalised score between 0 - 1.0
            bool isHit = false;
            double frameOffset = 1 / framesPerSec;
            int bottomFrequency = minHz; // units = Hertz
            int bottomBin = 0;

            // pass over all frequency bins except last two due to edge effect later.
            for (int i = 0; i < binCount - 2; i++)
            {
                if (isHit == false && trackIntensityArray[i] >= scoreThreshold)
                {
                    //low freq end of a track event
                    isHit = true;
                    bottomBin = i;
                    bottomFrequency = minHz + (int)Math.Round(i * freqBinWidth);
                }
                else // check for the high frequency end of a track event
                if (isHit && trackIntensityArray[i] <= scoreThreshold)
                {
                    // now check if there is acoustic intensity in next two frequncy bins
                    double avIntensity = (trackIntensityArray[i] + trackIntensityArray[i + 1] + trackIntensityArray[i + 2]) / 3;

                    if (avIntensity >= scoreThreshold)
                    {
                        // this is not top of vertical track - it continues through to higher frequency bins.
                        continue;
                    }

                    // bin(i - 1) is the upper Hz end of an event, so initialise it
                    isHit = false;
                    double eventBinWidth = i - bottomBin;
                    double hzBandwidth = (int)Math.Round(eventBinWidth * freqBinWidth);

                    //skip events having wrong bandwidth
                    if (hzBandwidth < minBandwidth || hzBandwidth > maxBandwidth)
                    {
                        continue;
                    }

                    // obtain an average score for the bandwidth of the potential event.
                    double av = 0.0;
                    for (int n = bottomBin; n <= i; n++)
                    {
                        av += trackIntensityArray[n];
                    }

                    av /= eventBinWidth;

                    // Initialize the event with: TimeSpan segmentStartOffset, double eventStartSegmentRelative, double eventDuration, etc
                    // Vertical track events are assumed to be two frames duration.  FIX THIS FIX THIS ################################################
                    double eventDuration = frameOffset * 2;
                    double startTimeRelativeSegment = frameOffset * frameNumber;
                    var ev = new AcousticEvent(segmentStartOffset, startTimeRelativeSegment, eventDuration, bottomFrequency, bottomFrequency + hzBandwidth);
                    ev.SetTimeAndFreqScales(frameOffset, freqBinWidth);
                    ev.Score = av;

                    // normalised to the user supplied threshold
                    ev.ScoreNormalised = ev.Score / maxPossibleScore;
                    if (ev.ScoreNormalised > 1.0)
                    {
                        ev.ScoreNormalised = 1.0;
                    }

                    ev.Score_MaxPossible = maxPossibleScore;

                    //find max score
                    double max = -double.MaxValue;
                    for (int n = bottomBin; n <= i; n++)
                    {
                        if (trackIntensityArray[n] > max)
                        {
                            max = trackIntensityArray[n];
                            ev.Score_MaxInEvent = trackIntensityArray[n];
                        }
                    }

                    events.Add(ev);
                }
            }

            return events;
        }
    }
}
