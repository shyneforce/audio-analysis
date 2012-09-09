﻿module Search


    (*
        New module
        Objective: search for events

        Three stages:
        1) template construction
        2) point of interest detection
        3) POI classification (as a template)


        1) make use of work already done
            - ensure at least 4 or more features are used
                - duration, startFreq, endFreq used for preprocessing

        2) based on AED... in principal easy enough

        3) is where the real work will needs to begin
            - // this is a classifier
            - load templates

            - load aed events

            - prep work
                - for each aed event
                        - get the nosie profile for the minute surrounding the event
                        - calculate centroid of aed event
            
                - for each template        
                    - calculate centroid
                
            - for each template (t)
                
                - for each aed event (ae)
                        - get the snippet of audio (sn) for those bounds. 
                            - from ae.centroid, cut out t.width, aligned by t.centroid
                            - add padding?
                        
                        - use freq bounds on template to apply a bandpass to sn
                            - padding?
                            - roll off response?

                        - run snippet through feature extraction sn => sn.features <- values
                        
                        - calculate classification metric (e.g. distance)
                            - from sn.features
                            - to t.features

                        - return tuple
                            (t.ID, ae.ID,distance)
                    - POSSIBLE IMPROVEMENT
                        - for every n (n=?) pixels P in ae.bounds
                            - where template does not go over bounds
                            - slide template across all available positions
                            - minimum movement is n pixels (x / y) 


            - summarise results
                - for each result (r)
                    - return a list of likely ?species/calls?

    *)
    open System
    open System.IO
    open QutSensors.AudioAnalysis.AED.Util
    open Microsoft.FSharp.Core
    open Microsoft.FSharp.Math.SI
    open FELT
    open MQUTeR.FSharp.Shared
    open Microsoft.FSharp.Collections

    type Point<'a, 'b> = { x : 'a; y: 'b}
    
    module Point =
        let x p = p.x
        let y p = p.y
        let toTuple p = p.x, p.y

    let centroid (ae: Rectangle<float<_>,float<_>>) =
        {x = left ae + (ae.Width / 2.0) ; y = top ae  + (ae.Height / 2.0)}

    let inline centerToEdges center width =
        let h = LanguagePrimitives.DivideByInt width 2
        center - h, center + h

    let inline centroidToRect point width height=
        cornersToRect2 (centerToEdges point.x width) (centerToEdges point.y height)
    
    type EventRect = Rectangle<float<s>,float<Hz>>
    type Event = {
        AudioReadingId : Guid
        Bounds : EventRect
    }    

    let getNoiseProfile startOffset endOffset recordingID =
        
        raise <| new NotImplementedException()

    let cutSnippet sourceFile (center:TimeSpan) (duration:TimeSpan) (lowBand:Hertz) (highBand:Hertz) =
        
        // returns a wav

        raise <| new NotImplementedException()
    
    let snippetToSpectrogram wavSource =
        
        raise <| new NotImplementedException()

    let extractFeatures snippet =

        raise <| new NotImplementedException()

    let getTemplates path workflow =
        let fip = new FileInfo(path)
        if fip.Exists then
            
            use stream = fip.Open FileMode.Open
            let data : Data = Serialization.deserializeBinaryStream stream

            data
        else
            raise <| FileNotFoundException("The data file was not found: " + path, path)



    let remapBoundsOfAnEvent bounds event =
        let centerAndAlign bound =
            // to do: sense checking
            Some <| centroidToRect event (width bound) (height bound)
        Array.map (centerAndAlign) bounds
        |> Array.choose (id)


    let compareEvents eventA eventB =
        // some sort of classification

        3.0

    let compareTemplatesToEvent (templateData:Data) event =
        // import boundaries
        let getBound headers = cornersToRect 0.0<s> 0.0<s> 0.0<Hz> 0.0<Hz>
            
        let bounds : Rectangle<float<s>,float<Hz>>[] = templateData.Instances |> Map.scanAll |> snd |> Seq.map getBound |> Seq.toArray
        
        // create copies of the "event" with different bounds
        let overlays = remapBoundsOfAnEvent bounds event

        // for each overlay, extract stats
        let possibleEvents = Array.map extractFeatures 

        ()

    let main =
        
        let workflow = FELT.Workflows.Analyses.["???"]
        let pathToTrainingData = ""
        


        // trained templates
        let templateData = getTemplates pathToTrainingData workflow

        // run aed 
        let aedEvents = [||]

        let analysedEvents = Array.map (compareTemplatesToEvent templateData) aedEvents
         


        ()
