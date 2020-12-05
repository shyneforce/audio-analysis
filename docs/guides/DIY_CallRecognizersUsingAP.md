# Welcome to "DIY Recognizer"

File: DIY_CallRecognizersUsingAP.md
         
**DIY Recognizer** is a utility within **Analysis Programs**, a command line program that analyses long-duration audio-recordings of the enviornment. AnalysisPrograms.exe (abbreviated from here to AP) contains many sub-programs, one of which provides you with the ability to write your own call recognizers. This manual describes only how to write a **DIY recognizer**.

**Why bother with a DIY recognizer?**

There are three levels of sophistication in call recognizers:
1. The simplist is the handcrafted template.
2. More powerful is a machine learned model.
3. The current cutting edge of call recognizers is a deep-learning approach using a convolutional neural network.

A comparison of these recognizer types is shown in the next figure. Note that the two rules at the bottom of the figure are true regardless of the type of recognizer. 

![A comparison of three kinds of call recognizer](./Images/WriteYourOwnRecognizer.png)

Hand-crafted, *rule-based* templates can be built using just one or a few examples of the target call. But like any rule-based *AI* system they are *brittle*, that is, they break easily if the target call falls even slightly outside the bounds of the rules. A machine-learned model, for example an SVM or Random Forest, is far more resilient to slight changes in the range of the target call but they require many more training examples, on the order of 100 training examples. Finally, the convolutional neural network is the most powerful learning machine available today (2020) but this power is achieved only by supplying thousands of examples of the each target call.

To summarise (and at the risk of over-simplification), a hand-crafted template has low cost and low benefit; a machine-learned model has medium cost and medium benefit, while a deep-learned model has high cost and high benefit. The cost/benefit ratio in each case is similar but here is the catch - the cost must be paid before you get the benefit! Furthermore, in a typical ecological study, a bird species is of interest precisely because it is threatened or cryptic. When not many calls are available, the more sophisticated approaches become untenable. Hence there is a place for hand-crafted templates in call recognition.

![DIYCostBenefit](./Images/DIYCostBenefit.png)

**The advantages of a hand-crafted DIY call recognizer:**
1. You can do it yourself!
2. You can start with just one or two calls.
3. Allows you to collect a larger dataset for machine learning purposes.
4. Exposes the variability of the target call. 

## Calls, syllables, harmonics
The algorithmic approach of **DIY Recognizer** reflects particular assumptions about animals calls and how they are sturctured. A *call* is taken to be any sound of animal origin (whether for communication purposes or not) and includes bird songs/calls, animal vocalisations of any kind, the stridulation of insects, the wingbeats of birds and bats and the various sounds produced by acquatic animals. Calls typically have temporal and spectral structure. For example they may consist of a temporal sequence of two or more syllables (with "gaps" in between) or a set of simultaneous *harmonics* or *formants*. (The distinction between harmonics and formants does not concern us here.)

**DIY Recognizer** attempts to recognizer calls in a noise-reduced spectrogram, which is processed as a grey-scale image. Each row of pixels is a frqeuency bin and each column of pixels is a time-frame. The value in each spectrogram cell (represented by an image pixel) is the acoustic intensity in decibels with respect to the noise baseline. Note that the decibel values in a noise-reduced spectrogram are always positive.

## Acoustic events

An *acoustic event* is defined as a contiguous set of spectrogram cells/pixels whose decibel values exceed some user defined threshold. In an ideal case, an acoustic event should encompass a discrete component of acoustic energy within a call, syllable or harmonic. It will be separated from other acoustic events by intervening pixels having decibel values *below* the user defined threshold. **DIY Recognizer** contains algorithms to recognize several different kinds of acoustic event based on their shape in the spectrogram. We describe these in turn.

## Seven kinds of acoustic event

## 1: A Shreik
This is a diffuse acoustic event that is extended in both time and frequency. While a shriek may have internal structure, it is treated as a "blob" of acoustic energy. A typical example is a parrot shriek.

## 2: A Whistle
This is a narrow band, "pure" tone having duration over several to many time frames but having very restricted bandwidth. In theory a pure tone occupies a single frequency bin, but in practice bird whistles can occupy several freqeuncy bins and appear as a horizontal *spectral track* in the spectrogram.

## 3: A Chirp
This sounds like a whistle whose frequency increases or decreases over time. A chirp is said to be a *frqeuency modulated* tone. It appears in the spectrogram as a gently ascending or descending *spectral track*.

## 4: A Whip
A *whip* is like a *chirp* except that the frequency modulation can be extremely rapid so that it sounds like a "whip crack". It has the appearance of a steeply ascending or descending *spectral track* in the spectrogram. An archetypal whip is the final component in the whistle-whip of the Australian whip-bird. Within the DIY Recognizer software, the distinction between a chirp and a whip is not sharp. That is, a *spectral track* that is ascending diagonally (cell-wise) at 45 degrees in the spectrogram will be detected by both the *chirp* and the *whip* algorithms.

## 5: A Click
The *click* appears as a single vertical line in a spectrogram and sounds, like the name suggests, as a very brief click. In practice, depending on spectrogram configuration settings, a *click* may occupy two or more adjacent time-frames.

Note that each of the above five acoustic events are "simple" or "singular" events. The remaining two kinds of acoustic event are said to be composite, that is, they are composed of more than one acoustic event but the detection algorithm is designed to pick them up as singular events.

## 6: An Oscillation
An oscillation is the same (or nearly the same) syllable (typically whips or clicks) repeated at a fixed periodicity over several to many time-frames.

## 7: Harmonics
Harmonics are the same/similar shaped *whistle* or *chirp* repeated simultaneously at multiple intervals of frequency. Typically, the frequency interval is constant as one ascends the stack of harmonics.

## Configuration files
All the above seven types of acoustic event have distinct properties that define their temporal duration, bandwidth, intensity. In fact, an acoustic event is, by definition, enclosed by a rectangle/marquee whose height represents the bandwidth of the event and whose width represents the duration of the event. Even a chirp or whip which consists only of a single *spectral track*, is enclosed by a rectangle, two of whose vertices sit at the start and end of the track. 

## Five steps in a DIY call recognizer
1. Recording segmentation
2. Spectrogram preparation
3. Call syllable detection
4. Combining syllables into calls
5. Call filtering
6. Saving Results

In order to execute these steps correctly for your call of interest, you must enter suitable parameter values into a *config.yml* file. The name of this file is passed as an argument to AP.exe which reads the file and executes the recognition steps. The command line will be explained subsequently. We now describe how to set the parameters for each of the six recognition steps, using as a concrete example the config file for the Boobook Owl, *Ninox boobook*.
Note that the config filename must have the correct structure in order to be recognized by AP, in this case `Towsey.NinoxBoobook.yml`. `Towsey` is the author of the yml file (you can change this to your name) and `NinoxBoobook` is the scientific name (must be without spaces) of the target species. 

The parameters are described below. The parameter name is given in bold followed by a typical or default value for the parameter. A description of the parameter is given on the following line(s).

## Step 1. Recording Segmentation
There are two parameters that determine how a long recording is segmented:   
**SegmentDuration:** 60    
**SegmentOverlap:** 0    
The default values are 60 and 0 seconds respectively and these seldom need to be changed. You may wish to work at finer resolution by changing SegmentDuration to 20 or 30 seconds. If your target call is comparitively long (e.g. greater than 10 - 15 seconds), you could increase SegmentDuration to 70 seconds and increase SegmentOverlap to 10 seconds. This reduces the probability that a call will be split across segments. It also maintains 60-second intervals between segment-starts which helps in identifying where you are in a recording.

## Step 2. Spectrogram preparation
There are four parameters that determine how a spectrogram is derived from each recording segment.
**ResampleRate:** 22050    
**FrameSize:** 1024    
**FrameStep:** 256    
**WindowFunction:** HANNING   
**BgNoiseThreshold:** 0   

**ResampleRate** must be twice the desired Nyquist. If your config file does not specify a value for ResampleRate, your recording will be up- or down-sampled to the default value of 22050 samples per second. As a rule of thumb, specify the resample rate that will give the best result given your recording sample rate. If the target call of interest is in a low frequency band (e.g. < 2kHz), then lower the resample rate to twice the maximum frequency of interest. This will reduce processing time and produce better focused spectrograms.

**FrameSize** and **FrameStep** determine the time and frequency resolution of the spectrogram. Typical values are 512 and 0 samples respectively. There is a trade-off between time resolution and frequency resolution; finding the best compromise is really a matter of trial and error. If your target call is of long duration with little temporal variation (e.g. a whistle) then **FrameSize** can be increased to 1024 or even 2048. (NOTE: The value of **FrameSize** must be a power of 2.) To capture more temporal variation in your target calls, decrease **FrameSize** and/or decrease **FrameStep**. A typical **FrameStep** might be half the **FrameSize** but does not need to be a power of 2.

The default value for **WindowFunction** is HANNING. There should no need to ever change this but you might like to try a HAMMING window if you are not satisfied with the appearance of your spectrograms.

**BgNoiseThreshold** "Bg" means *background*. This parameter determines the degree of severity of noise removal fomr the spectrogram. The units are decibels. 


![A comparison of three kinds of call recognizer](./Images/AcousticEventParameters.png)

## 3. Call syllable detection
 Each of these profiles will be analyzed
*IMPORTANT NOTE: The indentation in these config.yml files is very important and should be retained. 

 This profile is required for the species-specific recogniser and must have the current name.
**Profiles:**
    **BoobookSyllable:** !ForwardTrackParameters
        **ComponentName:** RidgeTrack 
        **SpeciesName:** NinoxBoobook
        
        *# min and max of the freq band to search
        **MinHertz:** 400          
        **MaxHertz:** 1100
        **MinDuration:** 0.17
        **MaxDuration:** 1.2
        *# Scan the frequency band at these thresholds
        **DecibelThresholds:**
            - 6.0
            - 9.0
            - 12.0

## 4. Combining syllables into calls
This step is the first of three *post-processing* steps.
### Step 4.1: Combine overlapping events
 - events derived from all profiles.
**CombineOverlappingEvents: true

### Step 4.2: Combine possible syllable sequences
Combine possible syllable sequences and filter on excess syllable count.
    SyllableSequence:
        CombinePossibleSyllableSequence: true
        SyllableStartDifference: 0.6
        SyllableHertzGap: 350
        FilterSyllableSequence: true
        SyllableMaxCount: 2
        ExpectedPeriod: 0.4		

## 5. Call filtering
    ### Step 5.1: Remove events whose duration lies outside 3 SDs of an expected value.
    **Duration:
        **ExpectedDuration:** 0.14
        **DurationStandardDeviation:** 0.01        

    ### Step 5.2: Remove events whose bandwidth is too small or large.
        Remove events whose bandwidth lies outside 3 SDs of an expected value.
    **Bandwidth:
        **ExpectedBandwidth:** 280
        **BandwidthStandardDeviation:** 40

    ### Step 5.3: Filter the events for excess activity in their sidebands, i.e. upper and lower buffer zones
	    Remove events that have excessive noise in their side-bands.
    **SidebandActivity:
        **LowerHertzBuffer:** 150
        **UpperHertzBuffer:** 400
        **MaxAverageSidebandDecibels:** 3.0

## 6. Saving Results
In this final part of the config file, you set parameters that determine what results are saved to file.

- There are three options for saving spectrograms (case-sensitive): [False/Never | True/Always | WhenEventsDetected]
*True* is useful when debugging but *WhenEventsDetected* is required for operational use.
**SaveSonogramImages:** WhenEventsDetected

- There are three options for saving intermediate data files (case-sensitive): [False/Never | True/Always | WhenEventsDetected]
  Typically you would save intermediate data files only for debugging, otherwise your output will be excessive.
**SaveIntermediateWavFiles:** Never
**SaveIntermediateCsvFiles:** False

- The final option is obsolete - ensure it remains set to False
**DisplayCsvImage:** False

- The following reference to a second config file is irrelevant to call recognizers. It can be ignored, but must be retained.
**HighResolutionIndicesConfig:** "../Towsey.Acoustic.HiResIndicesForRecognisers.yml"


## The different kinds of syllables/events




[title](https://www.example.com)

![alt text](image.jpg)

### Usage: AnalysisPrograms < action > options

Actions:
  list - Prints the available program actions
    << no arguments >>
  help - Prints the full help for the program and all actions
    EXAMPLE: help spt
           will print help for the spt action

    -a  -actionname  [string]  1




Environment variables:
    AP_PLAIN_LOGGING  [true|false]       Enable simpler logging - the default is value is `false`
Global options:
    -d    -debug      [switch]        *  Do not show the debug prompt AND automatically attach a debugger. Has no effect in RELEASE builds
    -n    -nodebug    [switch]        *  Do not show the debug prompt or attach a debugger. Has no effect in RELEASE builds
    -l    -loglevel   [logverbosity]  *  Set the logging. Valid values: None = 0, Error = 1, Warn = 2, Info = 3, Debug = 4, Trace = 5, Verbose = 6, All = 7
    -v    -verbose    [switch]        *  Set the logging to be verbose. Equivalent to LogLevel = Debug = 4
    -vv   -vverbose   [switch]        *  Set the logging to very verbose. Equivalent to LogLevel = Trace = 4
    -vvv  -vvverbose  [switch]        *  Set the logging to very very verbose. Equivalent to LogLevel = ALL = 7

Actions:
  list - Prints the available program actions
    << no arguments >>
  help - Prints the full help for the program and all actions
    EXAMPLE: help spt
           will print help for the spt action

    -a  -actionname  [string]  1

