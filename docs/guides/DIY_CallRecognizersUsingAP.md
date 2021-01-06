`File: DIY_CallRecognizersUsingAP.md`
# Welcome to "DIY Call Recognizer"

         
> **DIY Call Recognizer** is a utility within **Analysis Programs**, a command line program that analyses long-duration audio-recordings of the enviornment. `AnalysisPrograms.exe` (abbreviated from here to `APexe`) contains many sub-programs or functions, one of which is the ability to write your own call recognizers. This manual describes how to write a **DIY call recognizer**. Refer to other manuals for other functionality [here](https://github.com/QutEcoacoustics/audio-analysis/blob/master/README.md).

## Contents ##
1. Why bother with a DIY call recognizer?
2. Calls, syllables, harmonics
3. Acoustic events
4. Detecting acoustic events
5. Configuration files
6. The list of parameters
7. An efficient strategy to tune parameters
8. Seven stages to building a DIY call recognizer
9. The command line
10. Building a larger data set

==============================================================

NOTE:
- Incomplete parts of the manual are indicated by _**TODO**_.
- Features not yet implemented are marked with a construction emoji (🚧). 

==============================================================


.


## 1. Why bother with a DIY call recognizer?

There are three levels of sophistication in call recognizers:
- The simplist is the handcrafted template.
- More powerful is a machine learned model.
- The current cutting edge of call recognizers is *deep-learning* using a convolutional neural network.

A comparison of these recognizer types is shown in the following table and explained further in the subsequent paragraph.

 ### **TABLE. A comparison of three different kinds of call recognizer**

| Type of Recognizer | Who does feature extraction? | Required dataset | Skill level | Accuracy |
|:---:|:---:|:---:|:---:|:---:|
|Template matching | User | Small (even 1!) | Least | Sometimes good |
|Supervised machine learning | User | Moderate (50-100s) | Some | Better |
|CNN | Part of CNN learning | Very large (10k to 1M) | A lot! | Best? |
||||


Hand-crafted, *rule-based* templates can be built using just one or a few examples of the target call. But like any rule-based *AI* system they are *brittle*, that is, they break easily if the target call falls even slightly outside the bounds of the rules. A supervised machine-learned model, for example an SVM or Random Forest, is far more resilient to slight changes in the range of the target call but they require many more training examples, on the order of 100 training examples. Finally, the convolutional neural network (CNN) is the most powerful learning machine available today (2020) but this power is achieved only by supplying thousands of examples of the each target call.

 **Note**: The following two rules apply to the preparation of training/test datsets, regardless of the recognizer type.

 - **Rule 1.** Rubbush in => rubbish out!! That is, think about your chosen training/test examples carefully.

-  **Rule 2.** Training and test sets should be representative (in some loose statistical sense) of the intended operational environment.


To summarise (and at the risk of over-simplification), a hand-crafted template has low cost and low benefit; a machine-learned model has medium cost and medium benefit, while a deep-learned model has high cost and high benefit. The cost/benefit ratio in each case is similar but here is the catch - the cost must be paid before you get the benefit! Furthermore, in a typical ecological study, a bird species is of interest precisely because it is threatened or cryptic. When not many calls are available, the more sophisticated approaches become untenable. Hence there is a place for hand-crafted templates in call recognition.

These thoughts are summarised in the following table:
| Type of Recognizer | Cost | Benefit | Cost/benefit ratio | The catch !|
|:---:|:---:|:---:|:---:|:---:|
|  Template matching | Low | Low | A number | You must pay ... |
|  Machine learning | Medium | Medium | A similar number | ... the cost before ... |
|CNN | High | High | A similar number | ... you get the benefit! |
||||



**The advantages of a hand-crafted DIY call recognizer:**
1. You can do it yourself!
2. You can start with just one or two calls.
3. Allows you to collect a larger dataset for machine learning purposes.
4. Exposes the variability of the target call. 

.

.

## 2. Calls, syllables, harmonics
The algorithmic approach of **DIY Call Recognizer** makes particular assumptions about animals calls and how they are structured. A *call* is taken to be any sound of animal origin (whether for communication purposes or not) and includes bird songs/calls, animal vocalisations of any kind, the stridulation of insects, the wingbeats of birds and bats and the various sounds produced by acquatic animals. Calls typically have temporal and spectral structure. For example they may consist of a temporal sequence of two or more *syllables* (with "gaps" in between) or a set of simultaneous *harmonics* or *formants*. (The distinction between harmonics and formants does not concern us here.)

**DIY Call Recognizer** attempts to recognizer calls in a noise-reduced spectrogram, which is processed as a matrix of real values but visualised as a grey-scale image. Each row of pixels is a frqeuency bin and each column of pixels is a time-frame. The value in each spectrogram/matrix cell (represented visually by an image pixel) is the acoustic intensity in decibels with respect to the background noise baseline. Note that the decibel values in a noise-reduced spectrogram are always positive.

.

.



## 3. Acoustic events

An *acoustic event* is defined as a contiguous set of spectrogram cells/pixels whose decibel values exceed some user defined threshold. In the ideal case, an acoustic event should encompass a discrete component of acoustic energy within a call, syllable or harmonic. It will be separated from other acoustic events by intervening pixels having decibel values *below* the user defined threshold. **DIY Call Recognizer** contains algorithms to recognize seven different kinds of acoustic event based on their shape in the spectrogram. We describe these in turn.

### 3.1. Shreik 
This is a diffuse acoustic event that is extended in both time and frequency. While a shriek may have some internal structure, it is treated by **DIY Call Recognizer** as a "blob" of acoustic energy. A typical example is a parrot shriek.

### 3.2. Whistle
This is a narrow band, "pure" tone having duration over several to many time frames but having very restricted bandwidth. In theory a pure tone occupies a single frequency bin, but in practice bird whistles can occupy several freqeuncy bins and appear as a horizontal *spectral track* in the spectrogram.

### 3.3. Chirp
This sounds like a whistle whose frequency increases or decreases over time. A chirp is said to be a *frqeuency modulated* tone. It appears in the spectrogram as a gently ascending or descending *spectral track*.

### 3.4. Whip
A *whip* is like a *chirp* except that the frequency modulation can be extremely rapid so that it sounds like a "whip crack". It has the appearance of a steeply ascending or descending *spectral track* in the spectrogram. An archetypal whip is the final component in the whistle-whip of the Australian whip-bird. Within the DIY Recognizer software, the distinction between a chirp and a whip is not sharp. That is, a *spectral track* that is ascending diagonally (cell-wise) at 45 degrees in the spectrogram will be detected by both the *chirp* and the *whip* algorithms.

### 3.5. Click
The *click* appears as a single vertical line in a spectrogram and sounds, like the name suggests, as a very brief click. In practice, depending on spectrogram configuration settings, a *click* may occupy two or more adjacent time-frames.

Note that each of the above five acoustic events are "simple" or "singular" events. The remaining two kinds of acoustic event are said to be composite, that is, they are composed of more than one acoustic event but the detection algorithm is designed to pick them up as singular events.

### 3.6. Oscillations
An oscillation is the same (or nearly the same) syllable (typically whips or clicks) repeated at a fixed periodicity over several to many time-frames.

### 3.7. Harmonics
Harmonics are the same/similar shaped *whistle* or *chirp* repeated simultaneously at multiple intervals of frequency. Typically, the frequency intervals are similar as one ascends the stack of harmonics.

![Seven Kinds Of Acoustic Event](./Images/SevenKindsOfAcousticEvent.png)

.

.

## 4. Detecting acoustic events
**DIY Call Recognizer** detects or recognizes target calls in an audio recording using seven steps: 
1. Audio resampling
2. Audio segmentation
3. Spectrogram preparation
4. Call syllable detection
5. Combining syllable events into calls
6. Call filtering
7. Saving Results

It helps to group these steps into three parts:
- Steps 1 and 2: _Pre-processing_ steps to prepare the recording for subsequent analysis.
- Steps 3 and 4: _Processing_ steps to encapsulate target syllables as acoustic events. 
- Steps 5 to 7: _Post-processing_ steps which simplify the output from step 4 by combining related events, filtering events to remove false-positives and finally saving the remainder. 

To execute these seven steps correctly, you must enter suitable _parameter values_ into a _configuration file_. 

.

.

## 5. Configuration files
**DIY Call Recognizer** is a command line tool that requires a _configuration file_ (or _config_ file) in order to find calls of interest in a recording. The name of the config file is included as a command line argument. `APexe` reads the file containing a list of recognition _parameters_ and then executes the recognition steps accordingly. The command line will be described in a subsequent section. 

> NOTE: The config filename must have the correct structure in order to be recognized by `APexe`. For example, if the config file name is `Ecosounds.NinoxBoobook.yml`:
> - `Ecosounds` tells `APexe` that this is a call recognition task.
> - `NinoxBoobook` tells `APexe` the scientific name of the target species, in this case the Boobook owl. (Note there must no spaces in the file name.)
> - `.yml` informs `APexe` what syntax to expect, in this case YAML.



`APexe` config files must be written in a language called YAML. For an introduction to YAML syntax please see this article: https://sweetohm.net/article/introduction-yaml.en.html. 
We highly recommend using Notepad++ or Visual Studio Code to edit your YAML config files. Both are free, and both come with built in syntax highlighting for YAML files.

### Parameters
Config files contain a list of parameters, each of which is written as a name-value pair, for example:
```yml
SampleRate: 22050
```

Note that the parameter name `SampleRate` is followed by a colon, a space and then a value for the parameter. In this manual we will use typical or default values as examples. Obviously, the values must be "tuned" to the target calls. 


In order to be read correctly, the 20 or more parameters in a config file must be nested correctly. They are typially ordered according to the seven recognition steps, that is:
 
- Parameters that determine the pre-processing steps
- Parameters that define target acoustic events.
- Parameters that determine post-processing of the retrieved acoustic events.


### Profiles

Note that a config file may target more than one syllable or acoustic event. The parameters that describe a single acoustic event are grouped into what is called a _profile_. And all the profiles in a config file are listed under the heading or _key word_, `Profiles`. So we have a three level hierarchy:
1. the _profile list_ headed by the key-word `Profiles`.
2. the _profile_ headed by the profile name and event type.
3. the profile _parameters_ consisting of a list of name:value pairs. 

Here is an example:
```yml
Profiles:  
    BoobookSyllable1: !ForwardTrackParameters
        # min and max of the freq band to search
        MinHertz: 400          
        MaxHertz: 1100
        # min and max time duration of call
        MinDuration: 0.1
        MaxDuration: 0.499
    BoobookSyllable2: !ForwardTrackParameters
        MinHertz: 400          
        MaxHertz: 1100
        MinDuration: 0.5
        MaxDuration: 0.899
    BoobookSyllable3: !ForwardTrackParameters
        MinHertz: 400          
        MaxHertz: 1100
        MinDuration: 0.9
        MaxDuration: 1.2
```

In this artificial example, three profiles (i.e. syllables or acoustic events) are defined. Each profile has a user defined name (eg. BoobookSyllable3) and type. The `!` following the colon should be read as "of event type".  Each profile has four parameters. (The lines starting with `#` are comments and ignored by the yaml interpreter.) All three profiles have the same values for `MinHertz` and `MaxHertz` but different values for their time duration. 
 Note: Each profile is processed separately by `APexe`.


> *IMPORTANT NOTE: In YAML syntax, the levels of a hierarchy are distinguished by indentation alone. It is extremely important that the indentation is retained or the config file will not be read correctly. Use four spaces for indentation, not the TAB key.

**_TODO_** need to work on a description of the PROFILE TYPES e.g. `ForwardTrackParameters`, etc. 

 An additional note about acoustic events:
> All acoustic events are characterised by common properties, such as their temporal duration, bandwidth, decibel intensity. In fact, every acoustic event is bounded by an _implicit_ rectangle or marquee whose height represents the bandwidth of the event and whose width represents the duration of the event. Even a chirp or whip which consists only of a single sloping *spectral track*, is enclosed by a rectangle, two of whose vertices sit at the start and end of the track.

.

.

## 6. The list of parameters

This section describes how to set the parameters (using correct yaml syntax) for each of the seven recognition steps. We use, as a concrete example, the config file for the Boobook Owl, *Ninox boobook*.

The `YAML` lines are followed by an explanation of the parameters.

### Step 1. Recording resampling
If this parameter is not specified in the config file, the default is to resample (up- or down-sample) the recording to 22050 samples per second. This has the effect of limiting the maximum frequency in the recording to 11025 Hertz.   
```yml
ResampleRate: 22050
```
> *ResampleRate* must be to twice the desired Nyquist. As a rule of thumb, specify the resample rate that will give the best result for your target call. If the target call is in a low frequency band (e.g. < 2kHz), then lower the resample rate to twice the maximum frequency of interest. This will reduce processing time and produce better focused spectrograms. If you down-sample, you will lose high frequency content. If you up-sample, there will be undefined "noise" in spectrograms above the Nyquist.

### Step 2. Recording segmentation
Analysis of long recordings is made tractable by breaking them into shorter (typically 60-second) segments.
```yml
SegmentDuration: 60    
SegmentOverlap: 0
```    
> The default values are 60 and 0 seconds respectively and these seldom need to be changed. You may wish to work at finer resolution by changing SegmentDuration to 20 or 30 seconds. If your target call is comparitively long (such as a koala bellow, e.g. greater than 10 - 15 seconds), you could increase SegmentDuration to 70 seconds and increase SegmentOverlap to 10 seconds. This reduces the probability that a call will be split across segments. It also maintains a 60-second interval between segment-starts which helps to identify where you are in a recording.

### Step 3. Spectrogram preparation
There are four parameters that determine how a spectrogram is derived from each recording segment.
```yml    
FrameSize: 1024    
FrameStep: 256    
WindowFunction: HANNING   
BgNoiseThreshold: 0  
``` 

> *FrameSize* and *FrameStep* determine the time/frequency resolution of the spectrogram. Typical values are 512 and 0 samples respectively. There is a trade-off between time resolution and frequency resolution; finding the best compromise is really a matter of trial and error. If your target call is of long duration with little temporal variation (e.g. a whistle) then *FrameSize* can be increased to 1024 or even 2048. (NOTE: The value of *FrameSize* must be a power of 2.) To capture more temporal variation in your target calls, decrease *FrameSize* and/or decrease *FrameStep*. A typical *FrameStep* might be half the *FrameSize* but does not need to be a power of 2.

> The default value for *WindowFunction* is `HANNING`. There should no need to ever change this but you might like to try a `HAMMING` window if you are not satisfied with the appearance of your spectrograms.

> *BgNoiseThreshold* "Bg" means *background*. This parameter determines the degree of severity of noise removal from the spectrogram. The units are decibels. Zero is the safest default value and probably does not need to be changed. Increasing the value to say 3-4 decibels increases the likelihood that you will lose some important compoents of you target calls. For more on the noise removal algorithm used by `APexe` see [Towsey, Michael W. (2013) Noise removal from wave-forms and spectrograms derived from natural recordings of the environment.](https://eprints.qut.edu.au/61399/). 


![Templates have parameters](./Images/TemplatesHaveParameters.png)

### Step 4. Call syllable detection

Here is the declaration of the `Profiles` section in the `Ecosounds.NinoxBoobook.yml` file. It contains just one profile. The profile is named `BoobookSyllable` and is declared as type `ForwardTrackParameters` (a chirp). The `!` is shorthand for "of type". Indented below the profile declaration are seven properties (`name:value` pairs).
```yml
Profiles:  
    BoobookSyllable: !ForwardTrackParameters

        ComponentName: RidgeTrack 
        SpeciesName: NinoxBoobook
        
        # min and max of the freq band to search
        MinHertz: 400          
        MaxHertz: 1100
        MinDuration: 0.17
        MaxDuration: 1.2

        # Scan the frequency band at these thresholds
        DecibelThresholds:
            - 6.0
            - 9.0
            - 12.0
```

> **_TODO_** a description of the above parrameters. 

### Step 5. Combining syllables into calls
This step is the first of three *post-processing* steps.
IMPORTANT NOTE: These post-processing steps are applied to the calls collected from all *profiles* in the list of profiles. 

**Step 5.1.** Combine overlapping events. This is typically set *true*, but it depends on the target call. You may wish to set this true to remove certain kinds of multi-syllable calls.
 ```yml
PostProcessing:
    CombineOverlappingEvents: true
```
> **_TODO_** a description of the above parrameter. 

**Step 5.2.** Combine possible syllable sequences
Combine possible syllable sequences and filter on excess syllable count. Note the indentation.
```yml
    SyllableSequence:
        CombinePossibleSyllableSequence: true
        SyllableStartDifference: 0.6
        SyllableHertzGap: 350
        FilterSyllableSequence: true
        SyllableMaxCount: 2
        ExpectedPeriod: 0.4
```	

> **_TODO_** a description of the above parrameters. 

### Step 6. Call filtering
**Step 6.1.** Remove events whose duration lies outside 3 SDs of an expected value.

```yml
    Duration:
        ExpectedDuration: 0.14
        DurationStandardDeviation: 0.01
```
> **_TODO_** a description of the above parrameters. 

**Step 6.2.** Remove events whose bandwidth is too small or large.
        Remove events whose bandwidth lies outside 3 SDs of an expected value.
```yml
    Bandwidth:
        ExpectedBandwidth: 280
        BandwidthStandardDeviation: 40
```
> **_TODO_** a description of the above parrameters. 

**Step 6.3.** Filter the events for excess activity in their sidebands, i.e. upper and lower buffer zones
	    Remove events that have excessive noise in their side-bands.
```yml
    SidebandActivity:
        LowerHertzBuffer: 150
        UpperHertzBuffer: 400
        MaxAverageSidebandDecibels: 3.0
```
> **_TODO_** a description of the above parrameters. 

### Step 7. Saving Results
In this final part of the config file, you set parameters that determine what results are saved to file.

- There are three options for saving spectrograms (case-sensitive): [False/Never | True/Always | WhenEventsDetected]
*True* is useful when debugging but *WhenEventsDetected* is required for operational use.

```yml
SaveSonogramImages: WhenEventsDetected
```

- There are three options for saving intermediate data files (case-sensitive): [False/Never | True/Always | WhenEventsDetected]. 
  Typically you would save intermediate data files only for debugging, otherwise your output will be excessive.

```yml
SaveIntermediateWavFiles: Never
SaveIntermediateCsvFiles: False
```
- The final option is obsolete - ensure it remains set to False
```yml
DisplayCsvImage: False
```

- The following reference to a second config file is irrelevant to call recognizers. It can be ignored, but must be retained.
```yml
HighResolutionIndicesConfig "../File.Name.HiResIndicesForRecognisers.yml"
```

.

.



## 7. An efficient strategy to tune parameters

It will save you a lot of time if you tune the parameters in a logical sequence. The idea is to tune parameters in the sequence in which they appear in the *config.yml* file, keeping all "downstream" parameters as broad as possible. Here we summarize the strategy in five steps.

**Step 1.**
Turn off all post-processing steps. That is, turn all post-processing booleans to false OR comment out the keys in the `config.yml` file. 

**Step 2.**
    Initially set all profile parameters so has to catch the maximum possible number of target calls/syllables.

    Step 2a. Set the array of decibel thresholds to cover the expected range of call amplitudes from minimum to maxumum decibels.

    Step 2b. Set the minimum and maximum duration values to catch every target call by a wide margin. At this stage, do not worry that you are also catching a lot of false-positive events.

    Step 2c. Set the minimum and maximum frequency bounds to catch every target call by a wide margin. Once again, do not worry that you are also catching a lot of false-positive events.

    Step 2d. Set other parameters to their least "restrictive" values in order to catch maximum possible target events.

At this point you should have "captured" all the calls/syllables of interest (i.e. there should be no or minimum possible false-negatives) BUT you are likely to have many false-positives.

**Step 3.** Gradually constrain the parameter bounds (i.e. increase minimum values and decrease maximum values) until you start to lose obvious target calls/syllables. Then back off so that once again you just capture all the target events - but you will still have several to many false-positives. 

**Step 4. Event combining:** You are now ready to do the *post-processing* of events. The first post-processing step is to combine events that are likely to be *syllables* that are part of the same *call*.

**Step 5: Event Filtering:** Now add in the event filters in the same seqeunce that they appear in the *config.yml* file. This sequence cannot currently be changed because it is determined by the underlying code. There are event filters for duration, bandwidth, periodicity of component syllables within a call and finally acoustic activity in the side bands of an event.

    Step 5a. Filtering events on their time duration: duration.

    Step 5b. Filtering events on their bandwidth: bandwidth.

    Step 5c. Filtering events on the periodicity of component syllables within a call: periodicity of component syllables within a call.

    Step 5d. Filtering events on the acoustic activity in their side bands:  acoustic activity in the side bands of an event.

At the end of this process, you are likely to have a mixture of true-positives, false-postives and false-negatives. The goal is to set the parameter values so that the combined FP+FN total is minimised. You should adjust parameter values so that the final FN/FP ratio reflects the relative costs of FN and FP errors. For example, lowering a decibel threshold may pick up more TPs but almost certainly at the cost of more FPs. 

**NOTE:** A working DIY Call Recognizer can be built with just one or a few example/training call(s). A machine learning algorithm requires typically 100 true and false examples. The price you (the ecologist) pay for this simplicity is that you must exercise some of the "intelligence" that would be exercised by the machine learning algorithm. That is, you must select calls and set parameter values that reflect the variability of the target calls and also reflect the relative costs of FN and FP errors.

.

.



## 8. Eight steps to building a DIY Call Recognizer

**Step 1.** Select several one-minute recordings that contain typical examples of your target call. It is also desirable that the background acoustic events in you chosen recordings are representative of the intended operational environment. If this is difficult, one trick to try is to play examples of your target call through a loud speaker in a location that is similar to your intended operational enviornment. You can then record the calls using your intended Acoustic Recording Unit (ARU).

**Step 2.** Assign parameter values into your config.yml file for that species. A suggested file name would be: `DIYRecognizerConfig.SpeciesName.yml`.

**Step 3.** Run the recognizer. Use the command line described below.

**Step 4.** Review the detection accuracy and try to determine reasons for FP and FN detections.

**Step 5.** Change parameter values in order to increase the detection accuracy.

**Step 6.** Repeat stages 3, 4 and 5 until you are happy with the overall accuracy. In order to minimise the number of iteration of stages 3 to 5, it is best to tune the configuration parameters in the sequence previously described.

**Step 7.** At this point you should have a recognizer that performs "reasonably well" on your training examples. The next step is to test your recognizer on one or a few examples that it has not seen before. That is repeat steps 3, 4, 5 and 6 adding in a new example each time as they become available. It is also useful at this stage to accumulate a set of recordings that do *not* contain the target call. See Section 8 for more suggestions on building datasets. 

**Step 8:** At some point you are ready to use your recognizer on recordings obtained from the operational environment.

.

.


## 9. The DIY Call Recognizer command line
AnalysisPrograms.exe action arguments options

```powershell
    # prepare the arguments
    $file = ""
    $configFile = ""
    $outputDirectory = ""

    # prepare command line
    $command = " .\AnalysisPrograms.exe audio2csv $file $configFile 
                        $outputDirectory -n --quiet"

    # EXECUTE the command
    Invoke-Expression $command
```

### Actions:
- list - Prints the available program actions. No arguments required.

- help - Prints the full help for the program and all actions
    
    EXAMPLE: `help spt` will print help for the `spt` action

    -a  -actionname  [string]  1
    	#$command = ".\AnalysisPrograms.exe -h"
		#$command = " .\AnalysisPrograms.exe audio2csv --help"
		#Use these command lines to get help with constructing the command line.
		#$command = " `"$analysisProgramsDirectory\AnalysisPrograms.exe`" `"audio2csv`" --help"

### Global options:

    -d    -debug      [switch]      * Do not show the debug prompt AND automatically attach a debugger. Has no effect in RELEASE builds
    -n    -nodebug    [switch]      * Do not show the debug prompt or attach a debugger. Has no effect in RELEASE builds
    -l    -loglevel   [logverbosity] * Set the logging.
                Valid values:   None = 0,
                                Error = 1,
                                Warn = 2,
                                Info = 3,
                                Debug = 4,
                                Trace = 5,
                                Verbose = 6,
                                All = 7
    -v    -verbose    [switch]  * Set the logging to be verbose.
                                Equivalent to LogLevel = Debug = 4
    -vv   -vverbose   [switch]  * Set the logging to very verbose.                                      Equivalent to LogLevel = Trace = 4
    -vvv  -vvverbose  [switch]  * Set the logging to very very verbose.
                                Equivalent to LogLevel = ALL = 7


.

.



## 10. Building a larger data set

As indicated in Section 6, (*Eight steps to building a DIY Call Recognizer*), Step 7, it is useful to accumulate a set of recordings that both contain and *do not* contain the target call. The *negative* examples should contain acoustic events that have previously been detected as FPs. You now have two sets of recordings, one set containing the target call and one set containing previous FPs. The idea is to tune parameter values, while carefully watching for what effect the changes have on both data sets. Eventually, this data set could be used for machine learning purposes where you are not happy with the performance of the template recognizer.  

==================================================================


ADDITIONAL NOTES:

[title](https://www.example.com)

![alt text](image.jpg)


