`File: DIY_CallRecognizersUsingAP.md`
# Welcome to "DIY Call Recognizer"

         
> **DIY Call Recognizer** is a utility within **Analysis Programs**, a command line program that analyses long-duration audio-recordings of the environment. `AnalysisPrograms.exe` (abbreviated from here to `APexe`) can execute several different utlities or functions, one of which is the ability to write your own call recognizer. This manual describes how to write a **DIY call recognizer**. Refer to other manuals [here](https://github.com/QutEcoacoustics/audio-analysis/blob/master/README.md) for other utilities.

## Contents ##
1. Why bother with a DIY call recognizer?
2. Calls, syllables, harmonics
3. Acoustic events
4. Detecting acoustic events
5. Configuration files
6. Parameter names and values
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

There are three levels of sophistication in automated call recognizers:
- The simplist is the handcrafted template.
- More powerful is a _machine learned_ model.
- The current cutting edge of call recognizers is *deep-learning* using a convolutional neural network.

A comparison of these recognizer types is shown in the following table and explained further in the subsequent paragraph.

 ### **TABLE. A comparison of three different kinds of call recognizer**

| Type of Recognizer | Who does the feature extraction? | Required dataset | Skill level | Accuracy |
|:---:|:---:|:---:|:---:|:---:|
|Template matching | User | Small (even 1!) | Least | Sometimes good |
|Supervised machine learning | User | Moderate (50-100s) | Some | Better |
|CNN | Part of CNN learning | Very large (10k to 1M) | A lot! | Best? |
||||


Hand-crafted, *rule-based* templates can be built using just one or a few examples of the target call. But like any rule-based *AI* system, they are *brittle*, that is, they break easily if the target call falls even slightly outside the bounds of the rules. A supervised machine-learning model, for example an SVM or Random Forest, is far more resilient to slight changes in the range of the target call but they require many more training examples, on the order of 100 training examples. Finally, the convolutional neural network (CNN) is the most powerful learning machine available today (2021) but this power is achieved only by supplying thousands of examples of the each target call.

> **Note**: The following two rules apply to the preparation of training/test datasets, regardless of the recognizer type.

> - **Rule 1.** Rubbush in => rubbish out!! That is, think carefully about your chosen training/test examples.

> -  **Rule 2.** Training and test sets should be representative (in some loose statistical sense) of the intended operational environment.


To summarise (and at the risk of over-simplification), a hand-crafted template has low cost and low benefit; a machine-learned model has medium cost and medium benefit, while a deep-learned model has high cost and high benefit. The cost/benefit ratio in each case is similar but here is the catch - the cost must be paid _before_ you get the benefit! Furthermore, in a typical ecological study, a bird species is of interest precisely because it is threatened or cryptic. When not many calls are available, the more sophisticated approaches become untenable. Hence there is a place for hand-crafted templates in call recognition.

These ideas are summarised in the following table:
| Type of Recognizer | Cost | Benefit | Cost/benefit ratio | The catch !|
|:---:|:---:|:---:|:---:|:---:|
|  Template matching | Low | Low | A number | You must pay ... |
|  Machine learning | Medium | Medium | A similar number | ... the cost before ... |
|CNN | High | High | A similar number | ... you get the benefit! |
||||



**To summarise, the advantages of a hand-crafted DIY call recognizer are:**
1. You can do it yourself!
2. You can start with just one or two calls.
3. Allows you to collect a larger dataset for machine learning purposes.
4. Exposes the variability of the target call as you go. 


.

## 2. Calls, syllables, harmonics
The algorithmic approach of **DIY Call Recognizer** makes particular assumptions about animals calls and how they are structured. A *call* is taken to be any sound of animal origin (whether for communication purposes or not) and includes bird songs/calls, animal vocalisations of any kind, the stridulation of insects, the wingbeats of birds and bats and the various sounds produced by acquatic animals. Calls typically have temporal and spectral structure. For example they may consist of a temporal sequence of two or more *syllables* (with "gaps" in between) or a set of simultaneous *harmonics* or *formants*. (The distinction between harmonics and formants does not concern us here.)

**DIY Call Recognizer** attempts to recognize calls in a noise-reduced spectrogram, which is processed as a matrix of real values but visualised as a grey-scale image. Each row of pixels is a frqeuency bin and each column of pixels is a time-frame. The value in each spectrogram/matrix cell (represented visually by one image pixel) is the acoustic intensity in decibels with respect to the background noise baseline. Note that the decibel values in a noise-reduced spectrogram are always positive.

.


## 3. Acoustic events

An *acoustic event* is defined as a contiguous set of spectrogram cells/pixels whose decibel values exceed some user defined threshold. In the ideal case, an acoustic event should encompass a discrete component of acoustic energy within a call, syllable or harmonic. It will be separated from other acoustic events by intervening pixels having decibel values *below* the user defined threshold. **DIY Call Recognizer** contains algorithms to recognize seven different kinds of _"generic"_ acoustic event based on their shape in the spectrogram. We describe these in turn.

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

Note that each of the above five acoustic events are "simple" events. The remaining two kinds of acoustic event are said to be composite, that is, they are composed of more than one acoustic event but the detection algorithm is designed to pick them up as a single event.

### 3.6. Oscillations
An oscillation is the same (or nearly the same) syllable (typically whips or clicks) repeated at a fixed periodicity over several to many time-frames.

### 3.7. Harmonics
Harmonics are the same/similar shaped *whistle* or *chirp* repeated simultaneously at multiple intervals of frequency. Typically, the frequency intervals are similar as one ascends the stack of harmonics.

**Figure. The seven kinds of generic acoustic event**
![Seven Kinds Of Acoustic Event](./Images/SevenKindsAcousticEvent.jpg)

.


## 4. Detecting acoustic events
**DIY Call Recognizer** detects or recognizes target calls in an audio recording using a sequence of seven steps: 
1. Audio segmentation
2. Audio resampling
3. Spectrogram preparation
4. Call syllable detection
5. Combining syllable events into calls
6. Syllable/call filtering
7. Saving Results

It helps to group these detection steps into four parts:
- Steps 1 and 2: _Pre-processing_ steps to prepare the recording for subsequent analysis.
- Steps 3 and 4: _Processing_ steps to identify target syllables as _"generic"_ acoustic events. 
- Steps 5 and 6: _Post-processing_ steps which simplify the output from step 4 by combining related acoustic events and filtering events to remove false-positives. 
- Step 7: The final step is to save those events which remain.

To execute these seven detection steps correctly, you must enter suitable _parameter values_ into a _configuration file_. 



.

## 5. Configuration files
### The structure of the config file name
**DIY Call Recognizer** is a command line tool. It requires a _configuration file_ (henceforth, _config_ file) in order to find calls of interest in a recording. The name of the config file is included as a command line argument. `APexe` reads the file containing a list of _parameters_ and then executes the detection steps accordingly. The command line will be described in a subsequent section. 

> NOTE: The config filename must have the correct structure in order to be recognized by `APexe`. For example, given a config file with the name `AuthorId.GenericRecognizer.NinoxBoobook.yml`:
> - `AuthorId` is simply to keep track of the origins of the config.
> - `GenericRecognizer` tells `APexe` that this is a call recognition task and to parse the config file accordingly. Note this must be in second place in the file name.
> - `NinoxBoobook` (the Boobook owl) is an optional species name. `APexe` does not read/use this info but note that there must be no spaces in the file name.
> - `.yml` informs `APexe` what syntax to expect, in this case YAML.

**_TODO_** need to check with Anthony re changes to structure of the config file name.

`APexe` config files must be written in a language called YAML. For an introduction to YAML syntax please see this article: https://sweetohm.net/article/introduction-yaml.en.html. 
We highly recommend using Notepad++ or Visual Studio Code to edit your YAML config files. Both are free, and both come with built in syntax highlighting for YAML files.

### Parameters
Config files contain a list of parameters, each of which is written as a name-value pair, for example:
```yml
ResampleRate: 22050
```

Note that the parameter name `ResampleRate` is followed by a colon, a space and then a value for the parameter. In this manual we will use typical or default values as examples. Obviously, the values must be "tuned" to the target syllables. 


In order to be read correctly, the 20 or more parameters in a config file must be grouped and nested correctly. They are typically ordered according to the seven recognition steps above, that is:
 
- Parameters that determine pre-processing (detection steps 1 and 2)
- Parameters that describe the target syllables (detection steps 3 and 4)
- Parameters that determine post-processing of the retrieved acoustic events (steps 5 and 6)
- Parameters that determine saving of results (step 7)

### Profiles

A config file may target more than one syllable or acoustic event. The parameters that describe a single acoustic event are grouped into what is called a _profile_. And all the profiles in a config file are listed under the heading or _key word_, `Profiles`. So we have a three level hierarchy:
1. the _profile list_ headed by the key-word `Profiles`.
2. the _profile_ headed by the profile name (the key word) and the event type.
3. the profile _parameters_ consisting of a list of name:value pairs relevant to the profile.. 

Here is an (abbreviated) example:
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

This artificial example illustrates three profiles (i.e. syllables or acoustic events) under the key word `Profiles`. Each profile has a user defined name (eg. BoobookSyllable3) and type. The `!` following the colon should be read as "of event type".  Each profile in this example has four parameters. (The lines starting with `#` are comments and ignored by the yaml interpreter.) All three profiles have the same values for `MinHertz` and `MaxHertz` but different values for their time duration. Each profile is processed separately by `APexe`.


> *IMPORTANT NOTE ABOUT INDENTATION: In YAML syntax, the levels of a hierarchy are distinguished by indentation alone. It is extremely important that the indentation is retained or the config file will not be read correctly. Use four spaces for indentation, not the TAB key.

### Profile Types
In the above example the line `BoobookSyllable1: !ForwardTrackParameters` is to be read as "the name of the target syllable is "BoobookSyllable1" and its type is "ForwardTrackParameters". There are seven profile types corresponding to the seven kinds of acoustic event identified above. The event names are an attempt to describe what they sound like. But the corresponding profile type is descriptive of the algorithm used to find the event. This table lists the seven "generic" events and their corresponding profile types. It is vitally important that you define the correct profile type when write your own config file.

| Acoustic Event  | Type of the Corresponding Detection Algorithm |
|:---:|:---:|:---:|:---:|:---:|
|  Shriek | `!Blob` |
|  Whistle | `!HorizontalTrackParameters` |
|  Chirp   | `!ForwardTrackParameters` |
|  Whip    | `!UpwardsTrackParameters` |
|  Click   | `!VerticalTrackParameters` |
|  Oscillation | `!OscillationParameters` |
|  Harmonic | `!HarmonicParameters` |
||||


 ### An additional note about acoustic events
> All seven "generic" acoustic events are characterised by common properties, such as their minumum and maximum temporal duration, bandwidth, decibel intensity. In fact, every acoustic event is bounded by an _implicit_ rectangle or marquee whose height represents the bandwidth of the event and whose width represents the duration of the event. Even a _chirp_ or _whip_ which consists only of a single sloping *spectral track*, is enclosed by a rectangle, two of whose vertices sit at the start and end of the track.


.

## 6. Parameter names and values

This section describes how to set the parameters values (using correct yaml syntax) for each of the seven call-detection steps. We use, as a concrete example, the config file for the Boobook Owl, *Ninox boobook*.

The `YAML` lines are followed by an explanation of each parameter.

### Step 1. Audio segmentation
Analysis of long recordings is made tractable by breaking them into shorter (typically 60-second) segments.
```yml
SegmentDuration: 60    
SegmentOverlap: 0
```    
> The default values are 60 and 0 seconds respectively and these seldom need to be changed. You may wish to work at finer resolution by reducing _SegmentDuration_ to 20 or 30 seconds. If your target call is comparitively long (such as a koala bellow, e.g. greater than 10 - 15 seconds), you could 
increase _SegmentOverlap_ to 10 seconds. This actually increases the segment duration to 70 seconds (60+10) so reducing the probability that a call will be split across segments. It also maintains a 60-second interval between segment-starts, which helps to identify where you are in a recording.

### Step 2. Audio resampling
Specifies the sample rate at which the recording will be processed.   
```yml
ResampleRate: 22050
```
> If this parameter is not specified in the config file, the default is to _resample_ each recording segment (up or down) to 22050 samples per second. This has the effect of limiting the maximum frequency (the Nyquist) to 11025 Hertz.  *ResampleRate* must be twice the desired Nyquist. Specify the resample rate that gives the best result for your target call. If the target call is in a low frequency band (e.g. < 2kHz), then lower the resample rate to somewhat more than twice the maximum frequency of interest. This will reduce processing time and produce better focused spectrograms. If you down-sample, you will lose high frequency content. If you up-sample, there will be undefined "noise" in spectrograms above the original Nyquist.

**Figure. Parameters for the first three detection steps**
![First Three Detection Steps](./Images/ParametersForSteps1-3.png)

### Step 3. Spectrogram preparation

As noted above, the parameters for detection steps 3 and 4 are grouped into _profiles_ and multiple _profiles_ are nested under the keyword `Profiles`. The example below declares just one profile under the kepword `Profiles`. Its name is `BoobookSyllable` which is declared as type `ForwardTrackParameters` (a chirp). Indented below the profile declaration are its first six parameters.
```yml
Profiles:  
    BoobookSyllable: !ForwardTrackParameters
        SpeciesName: NinoxBoobook
        ComponentName: Chirp 
        FrameSize: 512
        FrameStep: 512
        WindowFunction: HANNING
        BgNoiseThreshold: 0.0
``` 

> The first two parameters, _SpeciesName_ and _ComponentName_, are optional. They assign descriptive names to the target species and syllable.

> The next four parameters determine how a spectrogram is derived from each recording segment. *FrameSize* and *FrameStep* determine the time/frequency resolution of the spectrogram. Typical values are 512 and 0 samples respectively. There is a trade-off between time resolution and frequency resolution; finding the best compromise is really a matter of trial and error. If your target syllable is of long duration with little temporal variation (e.g. a whistle) then *FrameSize* can be increased to 1024 or even 2048. (NOTE: The value of *FrameSize* must be a power of 2.) To capture more temporal variation in your target syllables, decrease *FrameSize* and/or decrease *FrameStep*. A typical *FrameStep* might be half the *FrameSize* but does *not* need to be a power of 2.

> The default value for *WindowFunction* is `HANNING`. There should never be a need to change this but you might like to try a `HAMMING` window if you are not satisfied with the appearance of your spectrograms.

> The "Bg" in *BgNoiseThreshold* means *background*. This parameter determines the degree of severity of noise removal from the spectrogram. The units are decibels. Zero sets the least severe noise removal. It is the safest default value and probably does not need to be changed. Increasing the value to say 3-4 decibels increases the likelihood that you will lose some important components of your target calls. For more on the noise removal algorithm used by `APexe` see [Towsey, Michael W. (2013) Noise removal from wave-forms and spectrograms derived from natural recordings of the environment.](https://eprints.qut.edu.au/61399/). 


### Step 4. Call syllable detection

A complete definition of the `BoobookSyllable` profile includes ten parameters, five for detection step 3 and five for step 4. The step 4 parameters direct the actual search for target syllables in the spectrogram.
```yml
Profiles:  
    BoobookSyllable: !ForwardTrackParameters
        ComponentName: RidgeTrack 
        SpeciesName: NinoxBoobook
        FrameSize: 512
        FrameStep: 512
        BgNoiseThreshold: 0.0
       
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

> _MinHertz_ and _MaxHertz_ define the frequency band in which a search is to be made for the target event. Note that these parameters define the bounds of the search band _not_ the bounds of the event itself. _MinDuration_ and _MaxDuration_ set the minimum and maximum time duration (in seconds) of the target event. At the present time these are hard bounds. 

**Figure. Common parameters for all acoustic events, using an oscillation event as example.**
![Common parameters](./Images/Fig2.png)


The above parameters are common to all target events. _Oscillations_ and _harmonics_, being more complex events, have additional parameters as described below. 

**_Oscillation Events_**

The algorithm to find oscillation events uses a _discrete cosine transform_ or *DCT*. Setting the correct DCT for the target syllable requires additional parameters. Here is the `Profiles` declaration in the config file for the _flying fox_. It contains two profiles, the first for a vocalisastion and the second to detect the rythmic sound of wing beats as a flying fox takes off or comes in to land. 
```yml
Profiles:
    Territorial: !BlobParameters
        ComponentName: TerritorialScreech
        MinHertz: 800          
        MaxHertz: 8000
        MinDuration: 0.15
        MaxDuration: 0.8
        DecibelThresholds:
            - 9.0
    Wingbeats: !OscillationParameters
        ComponentName: Wingbeats
        # The search band
        MinHertz: 200          
        MaxHertz: 2000
        # Min & max duration for sequence of wingbeats.
        MinDuration: 1.0
        MaxDuration: 10.0        
        DecibelThresholds:
            - 6.0
        # Wingbeat bounds - oscillations per second       
        MinOscillationFrequency: 4        
        MaxOscillationFrequency: 6    
        # DCT duration in seconds 
        DctDuration: 0.5
        # minimum acceptable value of a DCT coefficient
        DctThreshold: 0.5
        
        # Event threshold - use this to determine FP/FN trade-off.
        EventThreshold: 0.5
```
> Note the first six _wingbeat_ parameters are common to all events - parameters 2-6 determine the search band, the allowable event duration and the decibel threshold. The remaining five parameters determine the search for oscillations. _MinOscilFreq_ and _MaxOscilFreq_ specify the oscillation bounds in beats or oscillations per second. These values were established by measuring a sample of flying fox wingbeats. The next two parameters, the DCT duration in seconds and the DCT threshold can be tricky to establish but are critical for success. The DCT is computationally expensive but for accuracy it needs to span at least two or three oscillations. In this case a duration of 0.5 seconds is just enough to span at least two oscillations. The output from a DCT operation is an array of coefficients (taking values in [0, 1]). The index into the array is the oscillation rate and the value at that index is the amplitude. The index with largest amplitude indicates the likely oscillation rate, but _DctThreshold_ sets the minimum acceptable amplitude value. Lowering _DctThreshold_ increases the likelihood that random noise will be accepted as a true oscillation; increasing _DctThreshold_ increases the likelihood that a target oscillation is rejected.

> The optimum values for _DctDuration_ and _DctThreshold_ interact. It requires some experimentation to find the best values for your target syllable. Experiment with _DctDuration_ first while keeping the _DctThreshold_ value low. Once you have a reliable value for _DctDuration_, gradually increase the value for _DctThreshold_.
 
**Figure. Parameters required for using a DCT to detect an oscillation event.**
![DCT parameters](./Images/DCTparameters.jpg)


**_Harmonic Events_**

The algorithm to find harmonic events can be visualised as similar to the oscillations algorithm, but rotated by 90 degrees. It uses a DCT oriented in a vertical direction and requires similar additional parameters.

```yml
Profiles:
    Speech: !HarmonicParameters
        FrameSize: 512
        FrameStep: 512
        # The search band
        MinHertz: 500          
        MaxHertz: 5000
        # Min & max duration for a set of harmonics.
        MinDuration: 0.2
        MaxDuration: 1.0        
        DecibelThreshold: 2.0
        #  Min & max Hertz gap between harmonics
        MinFormantGap: 400        
        MaxFormantGap: 1200
        DctThreshold: 0.15         
        # Event threshold - use this to determine FP/FN trade-off.
        EventThreshold: 0.5
```
> Note there are only two parameters that are specific to _Harmonics_,  _MinFormantGap_ and _MaxFormantGap_. These specify the minimum and maximum allowed gap (measured in Hertz) between adjacent formants/harmonics. Note that for these purposes the terms _harmonic_ and _formant_ are equivalent. By default, the DCT is calculated over all bins in the search band.

> Once again, the output from a DCT operation is an array of coefficients (taking values in [0, 1]). The index into the array is the gap between formants and the value at that index is the formant amplitude. The index with largest amplitude indicates the likely formant gap, but _DctThreshold_ sets the minimum acceptable amplitude value. Lowering _DctThreshold_ increases the likelihood that random noise will be accepted as a true set of formants; increasing _DctThreshold_ increases the likelihood that a target set of formants is rejected.



### Step 5. Combining syllables into calls
Detection step 5 is the first of two *post-processing* steps. They both come under the keyword `PostProcessing`. Note that these post-processing steps are performed on all acoustic events collectively, i.e. all those "discovered" by all the *profiles* in the list of profiles. 

**Step 5.1.** Combine overlapping events. (Note the indentation)
 ```yml
PostProcessing:
    CombineOverlappingEvents: true
```
>This is typically set *true*, but it depends on the target call. You may wish to set this true for two reasons:
- the target call is composed of two or more overlapping syllables that you want to join as one event.
- whistle events often require this step to unite part-whistle detections as one event.


**Step 5.2.** Combine possible syllable sequences
 (Note the levels of indentation)
 ```yml
PostProcessing:
    SyllableSequence:
        CombinePossibleSyllableSequence: true
        SyllableStartDifference: 0.6
        SyllableHertzGap: 350
        FilterSyllableSequence: true
        SyllableMaxCount: 2
        ExpectedPeriod: 0.4
```	

> Set _CombinePossibleSyllableSequence_ true where you want to combine possible syllable sequences. A typical example is a sequence of chirps in a honeyeater call. _SyllableStartDifference_ and _SyllableHertzGap_ set the allowed latitude when combining events into sequences.  _SyllableStartDifference_ sets the maximum allowed time difference (in seconds) between the starts of two events. _SyllableHertzGap_ sets the maximum allowed frequency difference (in Hertz) between the minimum frequencies of two events.

> Once you have combined possible sequences, you may wish to remove sequences that do not satisfy the parameters for your target call. Set _FilterSyllableSequence_ true if you want to filter (remove) sequences that do not fall within the constraints defined by _SyllableMaxCount_ and _ExpectedPeriod_. _SyllableMaxCount_ sets an upper limit of the number of events that are combined to form a sequence and _ExpectedPeriod_ sets a limit on the average period (in seconds) of the combined events.

> **_TODO_** a description of how to set _ExpectedPeriod_ and how it works 


### Step 6. Call filtering ###


**Step 6.1.** Remove events whose duration lies outside an expected range.

```yml
PostProcessing:
    Duration:
        ExpectedDuration: 0.14
        DurationStandardDeviation: 0.01
```
> Note indentation of the key-word `Duration`. This filter removes events whose duration lies outside three standard deviations (SDs) of an expected value. _ExpectedDuration_ defines the _expected_ or _average_ duration (in seconds) for the target events and _DurationStandardDeviation_ defines _one_ SD of the assumed distribution. Assuming the duration is normally distributed, three SDs sets hard upper and lower duration bounds that includes 99.7% of instances. The filtering algorithm calculates these hard bounds and removes acoustic events that fall outside the bounds.

**Step 6.2.** Remove events whose bandwidth is too small or large.
        Remove events whose bandwidth lies outside 3 SDs of an expected value.
```yml
PostProcessing:
    Bandwidth:
        ExpectedBandwidth: 280
        BandwidthStandardDeviation: 40
```
> Note indentation of the key-word `Bandwidth`. This filter removes events whose bandwidth lies outside three standard deviations (SDs) of an expected value. _ExpectedBandwidth_ defines the _expected_ or _average_ bandwidth (in Hertz) for the target events and _BandwidthStandardDeviation_ defines one SD of the assumed distribution. Assuming the bandwidth is normally distributed, three SDs sets hard upper and lower bandwidth bounds that includes 99.7% of instances. The filtering algorithm calculates these hard bounds and removes acoustic events that fall outside the bounds.

**Step 6.3.** Remove events that have excessive noise in their side-bands.
```yml
PostProcessing:
    SidebandActivity:
        LowerHertzBuffer: 150
        UpperHertzBuffer: 400
        MaxAverageSidebandDecibels: 3.0
```
> Note indentation of the key-word `SidebandActivity`. This filter removes events that have acoustic activity in their sidebands (i.e. upper and lower buffer zones) exceeding the specified amount. The purpose of this filter is to remove _broadband_ events that encompass the frequency band of interest _and_ its sidebands for the event's duration _but are not the target event_. This is a common occurence, so this filter can be very useful. _LowerHertzBuffer_ and _UpperHertzBuffer_ define the bandwidth of the target sidebands. (These can be also be understood as buffer zones above and below the target event, hence the names assigned to the parameters.)

>  _MaxAverageSidebandDecibels_ is used in two ways: 1. it sets an upper limit on the acoustic energy in the sidebands (measured in Decibels and averaged over all spectorgram cells in the sidebands); and 2. sets an upper limit on the total acoustic energy in any one timeframe or frequency bin within the sidebands.

### Step 7. Saving Results
The parameters in this final part of the config file determine what results are saved to file. They do _not_ come under the `PostProcessing` keyword and therefore they are _not_ indented.

```yml
SaveSonogramImages: WhenEventsDetected
SaveIntermediateWavFiles: Never
SaveIntermediateCsvFiles: False
DisplayCsvImage: False
```

> There are three options for _SaveSonogramImages_:  [False/Never | True/Always | WhenEventsDetected] (These are case-sensitive.)
*True* is useful when debugging but *WhenEventsDetected* is required for operational use.

> There are three options for _SaveIntermediateWavFiles_ (also case-sensitive): [False/Never | True/Always | WhenEventsDetected]. 
  Typically you would save intermediate data files only for debugging, otherwise your output will be excessive.

> There are three options for _SaveIntermediateCsvFiles_ (also case-sensitive): [False/Never | True/Always | WhenEventsDetected]. 
  Typically this should be set false, otherwise your output will be excessive.

> The final parameter (_DisplayCsvImage_) is obsolete - ensure it remains set to False

The last parameter in the config file makes a reference to a second config file:
```yml
HighResolutionIndicesConfig: "../File.Name.HiResIndicesForRecognisers.yml"
```
This parameter is irrelevant to call recognizers and can be ignored, but it must be retained in the config file.


.


## 7. An efficient strategy to tune parameters

Tuning parameter values can be frustrating and time-consuming if a logical sequence is not followed. The idea is to tune parameters in the sequence in which they appear in the config file, keeping all "downstream" parameters as "open" or "unrestrictive" as possible. Here we summarize a tuning strategy in five steps.

**Step 1.**
Turn off all post-processing steps. That is, set all post-processing booleans to false OR comment out all post-processing keywords in the config file. 

**Step 2.**
    Initially set all profile parameters so as to catch the maximum possible number of target calls/syllables.

> Step 2a. Set the array of decibel thresholds to cover the expected range of call amplitudes from minimum to maxumum decibels.

> Step 2b. Set the minimum and maximum duration values to catch every target call by a wide margin. At this stage, do not worry that you are also catching a lot of false-positive events.

> Step 2c. Set the minimum and maximum frequency bounds to catch every target call by a wide margin. Once again, do not worry that you are also catching a lot of false-positive events.

> Step 2d. Set other parameters to their least "restrictive" values in order to catch maximum possible target events.

At this point you should have "captured" all the target calls/syllables (i.e. there should be minimal false-negatives), _but_ you are likely to have many false-positives.

**Step 3.** Gradually constrain the parameter bounds (i.e. increase minimum values and decrease maximum values) until you start to lose obvious target calls/syllables. Then back off so that once again you just capture all the target events - but you will still have several to many false-positives. 

**Step 4. Event combining:** You are now ready to set parameters that determine the *post-processing* of events. The first post-processing steps combine events that are likely to be *syllables* that are part of the same *call*.

**Step 5: Event Filtering:** Now add in the event filters in the same seqeunce that they appear in the config file. This sequence cannot currently be changed because it is determined by the underlying code. There are event filters for duration, bandwidth, periodicity of component syllables within a call and finally acoustic activity in the sidebands of an event.

> Step 5a. Set the _duration_ parameters for filtering events on their time duration.

> Step 5b. Set the _bandwidth_ parameters for filtering events on their bandwidth.

> Step 5c. Set the parameters for filtering based on _periodicity_ of component syllables within a call.

> Step 5d. Set the parameters for filtering based on the _acoustic activity in their side bands_.

At the end of this process, you are likely to have a mixture of true-positives, false-postives and false-negatives. The goal is to set the parameter values so that the combined FP+FN total is minimised. You should adjust parameter values so that the final FN/FP ratio reflects the relative costs of FN and FP errors. For example, lowering a decibel threshold may pick up more TPs but almost certainly at the cost of more FPs. 

> **NOTE:** A working DIY Call Recognizer can be built with just one example or training call. A machine learning algorithm typically requires 100 true and false examples. The price that you (the ecologist) pays for this simplicity is the need to exercise some of the "intelligence" that would otherwise be exercised by the machine learning algorithm. That is, you must select calls and set parameter values that reflect the variability of the target calls and the relative costs of FN and FP errors.

.


## 8. Eight steps to building a DIY Call Recognizer

We described above the various steps required to tune the parameter values in a recognizer config file. We now step back from this detail and take an overview of all the steps required to obtain an operational recognizer for one or more target calls. 

> **Step 1.** Select one or more one-minute recordings that contain typical examples of your target call. It is also desirable that the background acoustic events in your chosen recordings are representative of the intended operational environment. If this is difficult, one trick to try is to play examples of your target call through a loud speaker in a location that is similar to your intended operational environment. You can then record these calls using your intended Acoustic Recording Unit (ARU).

> **Step 2.** Assign parameter values into your config.yml file for the target call(s).


> **Step 3.** Run the recognizer, using the command line described in the next section.

> **Step 4.** Review the detection accuracy and try to determine reasons for FP and FN detections.

> **Step 5.** Tune or refine parameter values in order to increase the detection accuracy.

> **Step 6.** Repeat steps 3, 4 and 5 until you appear to have achieved the best possible accuracy. In order to minimise the number of iterations of stages 3 to 5, it is best to tune the configuration parameters in the sequence described in the previous section.

> **Step 7.** At this point you should have a recognizer that performs "as accurately as possible" on your training examples. The next step is to test your recognizer on one or a few examples that it has not seen before. That is, repeat steps 3, 4, 5 and 6 adding in a new example each time as they become available. It is also useful at this stage to accumulate a set of recordings that do *not* contain the target call. See Section 10 for more suggestions on building datasets. 

> **Step 8:** At some point you are ready to use your recognizer on recordings obtained from the operational environment.


.


## 9. The DIY Call Recognizer command line
`APexe` performs several functions or actions, each one requiring a different command line. In its most general form, the command line takes the form:

>`AnalysisPrograms.exe action arguments options` 

In this section we only describe the command line for the _call recognizer_ action where:
- action = "audio2csv".
- arguments = three file paths, to an audio file, a config file and an output directory. 
- options = short strings beginning with a single or double hyphen (`-` or `--`) that influence `APexe`'s execution.

Refer to other manuals [here](https://github.com/QutEcoacoustics/audio-analysis/blob/master/README.md) for a more complete description of `APexe`'s functionality. Note that the three file arguments must be in the order shown, that is: audio file, config file, output directory.

**Options:** There are three frequently useful options:

    1. The debug/no-debug options: Use "-d" for debug or "-n" for no debugging.
    2. The verbosity options: "--quiet", "-v", "-vv", "-vvv" for different levels of verbosity.
    3. The analysis-identifier option: Use "-a" or "--analysis-identifier" followed by the <analysis type>, which in the case of DIY call recognizers is "NameId.GenericRecognizer". This is a useful addition to the command line because it informs `APexe` that this as a call recognition task in case the config file is not named correctly.  
    
For other possible options, see the above referenced manual.

In powershell, the code to prepare and execute a commandline might look like this:
```powershell
    ...
    # prepare the arguments
    $audioFile = "path to the audio file"
    $configFile = "path to the config file"
    $outputDirectory = "path to the output directory"

    # prepare command line
    $command = " .\AnalysisPrograms.exe audio2csv $audioFile $configFile 
                    $outputDirectory -a NameId.GenericRecognizer -n --quiet"

    # EXECUTE the command
    Invoke-Expression $command
```
In the above command line, the options are no-debugging and minimal logging.

.


## 10. Building a larger data set

As indicated at Step 7 in Section 8 (*Eight steps to building a DIY Call Recognizer*), it is useful to accumulate a set of recordings, some of which contain the target call and some of which *do not*. The *negative* examples should include acoustic events that have previously been detected as FPs. You now have two sets of recordings, one set containing the target call(s) and one set containing previous FPs and other possible confusing acoustic events. The idea is to tune parameter values, while carefully watching for what effect the changes have on both data sets. Eventually, these two labelled data sets can be used for machine learning purposes.

In order to facilitate the determination of recognizer performance on labelled datasets, `APexe` can be run from the `Egret` software. `Egret` can greatly speed up the preparation of labelled datasets and can greatly improve the performance of a recognizer by more careful selection of positive and negative examples. `Egret` is available from  [https://github.com/QutEcoacoustics/egret](https://github.com/QutEcoacoustics/egret).  


==================================================================