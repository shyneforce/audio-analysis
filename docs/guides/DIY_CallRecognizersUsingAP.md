# Welcome to "DIY Call Recognizer" in Ecosounds Analysis Programs

File to be called <DIY_CallRecognizersUsingAP.md>.
         
**Analysis Programs** is a command line program to analyse long-duration audio-recordings of the enviornment. The AnalysisPrograms.exe (abbreviated here on in to AP) contains many sub-programs, one of which is the ability to write your own call recognizers.

##Why bother with a DIY call recognizer?
There are three levels of sophistication in call recognizers:
1. The simplist is the handcrafted template.
2. More powerful is a machine learning approach.
3. The current cutting edge of call recognizers is a deep-learning approach using a convolutional neural network.

Hand-crafted, *rule-based* templates can be built using just one or a few examples of the target call. But like any rule-based *AI* system they are *brittle*, that is, they break easily if the target call falls even slightly outside the bounds of the rules. A machine-learned model, for example an SVM or Random Forest, is far more resilient to slight changes in the range of the target call but they require many more training examples, on the order of 100 training examples. Finally, the convolutional neural network is most powerful learning machine availble today (2020) but this power is achieved only by supplying thousands of examples of the each target call. To summarise (and at the risk of over-simplification), a hand-crafted template has low cost and low benefit; a machine-learned model has medium cost and medium benefit, while a deep-learned model has high cost and high benefit. The cost/benefit in each case is similar but here is the rub - the cost must be paid before you get the benefit. Furthermore, in a typical ecological study, a bird is of interest precisely because the species is threatened or cryptgic - in other words not many calls are available, therefore making the more sophisticated approaches untenable. Hence there is a place for hand-crafted templates in call recognition. 

**The advantages of a hand-crafted DIY call recognizer:**
1. You can do it yourself!
2. You can start with just one or two calls.
3. Allowes you to collect a larger dataset for machine learning purposes.
4. Exposes the variability of the target call. 

## Five steps in a DIY call recognizer
1. Recording segmentation
2. Spectrogram preparation
3. Call syllable detection
4. Combining syllables into calls
5. Call filtering
6. Saving Results

In order to execute these steps correctly for your call of interest, you must enter suitable parameter values into a *config.yml* file. The name of this file is passed as an argument to AP.exe which reads the file and executes the recognition steps. The command line will be explained subsequently. We now describe how to set the parameters for each of the five recognition steps, using as a concrete example the config file for the Boobook Owl, *Ninox boobook*.
Note that the config filename must have the correct structure in order to be recognized by AP.exe, in this case `Towsey.NinoxBoobook.yml`. `Towsey` is the author of the yml file (you can change this to your name) and `NinoxBoobook` is the scientific name of the target species. 

## 1. Recording Segmentation

 SegmentDuration: units=seconds;    
**SegmentDuration:** 60
 SegmentOverlap: units=seconds;
**SegmentOverlap:** 0

## 2. Spectrogram preparation
  The resample rate must be twice the desired Nyquist. The default value is 22050 and your 
**ResampleRate:** 22050
**FrameSize:** 1024
**FrameStep:** 256
**WindowFunction:** HANNING


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

