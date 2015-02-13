﻿module AcousticEventDetectionTest

open QutSensors.AudioAnalysis.AED.AcousticEventDetection
open QutSensors.AudioAnalysis.AED.GetAcousticEvents
open QutSensors.AudioAnalysis.AED.Default
open Common
open FsCheckArbs
open Xunit
open FsUnit.Xunit
open QutSensors.AudioAnalysis.AED.Util

let sleDefaults = QutSensors.AudioAnalysis.AED.Default.largeAreaHorizontal

[<Fact>]
let testRemoveSubbandModeIntensities () =
    let f md =
        let i3 = loadTestFile "I2.csv" md |> removeSubbandModeIntensities
        let i3m = loadTestFile "I3.csv" md
        matrixFloatEquals i3 i3m 0.0001 |> Assert.True
    testAll f
    
[<Fact>]
let testToBlackAndWhite () =
    let f md =
        let i4 = loadTestFile "I3.csv" md |> toBlackAndWhite md.BWthresh
        let i4m = loadTestFile "I4.csv" md
        matrixFloatEquals i4 i4m 0.001 |> Assert.True
    testAll f
    
[<Fact>]
let testToFillIn () =
    let m = Array2D.create 5 10 0.0 |> Math.Matrix.ofArray2D
    Assert.False(toFillIn m 0 0 3)
    Assert.False(toFillIn m 1 9 3)
    m.[0,1] <- 1.0
    Assert.False(toFillIn m 0 0 3)
    Assert.False(toFillIn m 0 2 3)
    m.[0,3] <- 1.0
    Assert.True(toFillIn m 0 2 3)

(* TODO Investigate using FsCheck instead of xUnit for testJoinHorizontalLinesQuick
    forall m. forall i in m.NumRows. forall j in m.NumCols. m.[i,j] = 1 => (joinHorizontalLines m).[i,j] = 1
    m.[i,j] = 0 => [(m.[i,j] in gap => (joinHorizontalLines m).[i,j] = 1] xor (joinHorizontalLines m).[i,j] = 0
*)

[<Fact>]
let testJoinHorizontalLinesQuick () =
    let m = Math.Matrix.zero 5 10 
    Assert.Equal<matrix>(m, joinHorizontalLines m)
    m.[0,1] <- 1.0
    Assert.Equal<matrix>(m, joinHorizontalLines m)
    m.[0,2] <- 1.0
    Assert.Equal<matrix>(m, joinHorizontalLines m)
    m.[0,4] <- 1.0
    let m' = Math.Matrix.copy m
    m'.[0,3] <- 1.0
    Assert.Equal<matrix>(m', joinHorizontalLines m)
    
[<Fact>]
let testJoinHorizontalLines () =
    let f md =
        let i6b = loadTestFile "I5.csv" md |> joinHorizontalLines
        let i6bm = loadTestFile "I6.csv" md
        matrixFloatEquals i6b i6bm 0.001 |> Assert.True
    testAll f
    
[<Fact>]
let testJoinVerticalLines () =
    let f md =
        let i6a = loadTestFile "I4.csv" md |> joinVerticalLines
        let i6am = loadTestFile "I5.csv" md
        matrixFloatEquals i6a i6am 0.001 |> Assert.True
    testAll f

[<Fact>]
let aeToMatrixBounds () = chk (fun ae -> let m = aeToMatrix ae in m.NumRows = (height ae.Bounds) && m.NumCols = (width ae.Bounds))
    
[<Fact>]
let aeToMatrixElements () =
    let f ae i j x = 
        let inSet = Set.contains (ae.Bounds.Top+i, ae.Bounds.Left+j) ae.Elements
        if x = 1.0 then inSet else not inSet
    chk (fun ae -> aeToMatrix ae |> Math.Matrix.foralli (f ae))
    
[<Fact(Timeout=600000)>]
let testSeparateLargeEvents () =
    let f md =
        let ae2 = loadTestFile "I6.csv" md |> getAcousticEvents |> separateLargeEvents sleDefaults |> selectBounds
        let ae2m = loadIntEventsFile "AE2.csv" md
        Assert.Equal(Seq.length ae2m, Seq.length ae2)
        Assert.Equal<seq<_>>(Seq.sort ae2m, Seq.sort ae2)
    testAll f
    

    
[<Fact>]
let testSmallFirstMin () =
    let t = 42
    Assert.Equal(0, smallFirstMin [0..3] [1;2;1;2] t)
    Assert.Equal(0, smallFirstMin [0..2] [1;1;1] t)
    Assert.Equal(t, smallFirstMin [0..1] [2;1] t) 

[<Fact>]
let testSmallThreshold () =
    let f md =
        let ae2m = loadIntEventsFile "AE2.csv" md |> createEvents
        Assert.Equal(md.smallThreshOut, smallThreshold md.smallThreshIn ae2m)
    testAll f

[<Fact>]
let testFilterOutSmallEvents () =
    let f md =
        let ae2m = loadIntEventsFile "AE2.csv" md |> createEvents
        let ae3 = filterOutSmallEvents md.smallThreshIn ae2m |> selectBounds
        let ae3m = loadIntEventsFile "AE3.csv" md
        Assert.Equal<seq<_>>(Seq.sort ae3m, Seq.sort ae3)
    testAll f


module AcousticEventDetectionTestsForSeperateLargeEvents =
    let testMatrix = parseStringAsMatrix @"
00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000
00001111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111000000
00001111111111111111111111111111111111111111111111111110111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111000000
00001111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111000000
00000000000000000000000000000000000000000000111111111111111111000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000
00000000000000000000000000000000000000000000111111111111111111000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000
00000000000000000000000000000000000000000001111111111111100000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000
00000000000000000000000000000000000000000000000001111110000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000
00000000000000000000000000000000000000000000000111111110000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000
00000000000000000000000000000000000000000000000011111111000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000
00000000000000000000000000000000000000000000001111111110000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000
00000000000000000000000000000000000000000000000000011110000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000
00000000000000000000000000000000000000000000011111111111110000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000
00000000000000000000000000000000000000000000111111111111110000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000
00000000000000000000000000000000000000000000111111111111111000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000
00000000000000000000000000000000000000000000111111111111111110000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000
00000000000000000000000000000000000000000000111111111111111111000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000
00000000000000000000000000000000000000000000111111111111111110000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000
00000111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111110000000
00001111111111111111111111111111111111111111111111111111100000111111111111111111111111111111111111111111111111111100000111111111111111111111111111111111111111110000000
00001111111111111111111111111111111111111110000000001111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111110000000
00000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000
"       

    let expectedEvent1 = hitsToCoordinates << parseStringAsMatrix <| @"
011111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111
111111111111111111111111111111111111111111111111111110000011111111111111111111111111111111111111111111111111110000011111111111111111111111111111111111111111
111111111111111111111111111111111111111000000000111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111
"    
    
    let expectedEvent2 = hitsToCoordinates << parseStringAsMatrix <| @"
1111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111
1111111111111111111111111111111111111111111111111110111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111
1111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111
"    
    
    let expectedEvent3 = hitsToCoordinates << parseStringAsMatrix <| @"
1111111111111111111
1111111111110111111
1111111111111111111
0111111111111111111
0111111111111111111
1111111111111100000
0000001111110000000
0000111111110000000
0000011111111000000
0001111111110000000
0000000011110000000
0011111111111110000
0111111111111110000
0111111111111111000
0111111111111111110
0111111111111111111
0111111111111111110
1111111111111111111
1111111111111100000
0000000001111111111
"

    let expectedEvent3Alternate = hitsToCoordinates << parseStringAsMatrix <| @"
0111111111111111111
0111111111111111111
1111111111111100000
0000001111110000000
0000111111110000000
0000011111111000000
0001111111110000000
0000000011110000000
0011111111111110000
0111111111111110000
0111111111111111000
0111111111111111110
0111111111111111111
0111111111111111110
"

    let expectedBounds = 
        [
            lengthsToRect 4 18 156 3;
            lengthsToRect 4 1 157 3;
            lengthsToRect 43 1 19 20;
        ] :> seq<Rectangle<int, int>>

    let expectedAlternateBounds = lengthsToRect 43 4 19 14;
               
    [<Fact>]
    let ``seperate large events - testing  bounds`` () = 
        let results = testMatrix |> getAcousticEvents |> separateLargeEvents sleDefaults |> Array.ofSeq

        // expect threee events
        Assert.Equal(3, results.Length)

        // expect those three events to have the proper bounds
        Seq.iter2 (fun expected actualAcousticEvent -> Assert.Equal(expected, actualAcousticEvent.Bounds)) expectedBounds results

    [<Fact>]
    let ``seperate large events - hits returned for event 1`` () =
        let result = testMatrix |> getAcousticEvents |> separateLargeEvents sleDefaults|> Seq.nth 0
        
        let absoluteHits = Set.map (fun (y,x) -> (y + 18, x + 4)) expectedEvent1
        Assert.Equal<Set<_>>(absoluteHits, result.Elements)

    [<Fact>]
    let ``seperate large events - hits returned for event 2`` () =
        let result = testMatrix |> getAcousticEvents |> separateLargeEvents sleDefaults |> Seq.nth 1
        
        let absoluteHits = Set.map (fun (y, x) -> (y + 1, x + 4)) expectedEvent2
        Assert.Equal<Set<_>>(absoluteHits, result.Elements)

    [<Fact>]
    let ``seperate large events - hits returned for event 3`` () =
        let result = testMatrix |> getAcousticEvents |> separateLargeEvents sleDefaults |> Seq.nth 2
        
        let absoluteHits = Set.map (fun (y,x) -> (y + 1, x + 43)) expectedEvent3
        Assert.Equal<Set<_>>(absoluteHits, result.Elements)
    
    [<Fact>]
    let ``seperate large events - hits returned for event 3 when ExtrapolateBridgeEvents is diabled`` () =
        let sleParams = match sleDefaults with Horizontal p -> {p with ExtrapolateBridgeEvents = false} |> Horizontal
        let result = testMatrix |> getAcousticEvents |> separateLargeEvents sleParams|> Seq.nth 2
        
        let absoluteHits = Set.map (fun (y, x) -> (y + 4, x + 43)) expectedEvent3Alternate
        Assert.Equal<Set<_>>(absoluteHits, result.Elements)
        Assert.Equal(expectedAlternateBounds, result.Bounds)

        
// This is a fake matrix for testing whether separateLargeEvents works horizontally. 
module AcousticEventDetectionTestsForVerticalSeperateLargeEvents =
    let testMatrix = parseStringAsMatrix @"
0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000
0000111111111111111111111111111111111111111000000000000000000000111111111111111111111111111111111111
0000111111111111111111111111111111111111111000000000000000000000111111111111111111111111111111111111
0000111111111111111111111111111111111111111000000000000000000000111111111111111111111111111111111111
0000111111111111111111111111111111111111111000000000000000000000111111111111111111111111111111111000
0000111111111111111111111111111111111111111000000000000000000000111111111111111111111111111111111100
0000111111111111111111111111111111111111111000000000000000000000111111111111111111111111111111111100
0000111111111111111111111111111111111111111000000000000000000000111111111111111111111111111111111000
0000111111111111111111111111111111111111111000000000000000000000111111111111111111111111111111111000
0000111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111100
0000111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111000
0000111111111111111111111111111111111111111111111100000011111111111111111111111111111111111111111111
0000111111111111111111111111111111111111111000000000000000000000111111111111111111111111111111111111
0000111111111111111111111111111111111111111000000000000000000000111111111111111111111111111111111111
0000111111111111111111111111111111111111111000000000000000000000111111111111111111111111111111111111
0000111111111111111111111111111111111111111000000000000000000000111111111111111111111111111111111111
0000111111111111111111111111111111111111111000000000000000000000111111111111111111111111111111111111
0000111111111111111111111111111111111111111000000000000000000000111111111111111111111111111111111111
0000011111111111111111111111111111111111111000000000000000000000111111111111111111111111111111111111
0000111111111111111111111111111111111111111100000000000000000000011111111111111111111111111111111111
0000111111111111111111111111111111111111111000000000000000000000111111111111111111111111111111111111
0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000
"       
    let expectedEvent1 = hitsToCoordinates << parseStringAsMatrix <| @"
111111111111111111111111111111111111
111111111111111111111111111111111111
111111111111111111111111111111111111
111111111111111111111111111111111000
111111111111111111111111111111111100
111111111111111111111111111111111100
111111111111111111111111111111111000
111111111111111111111111111111111000
111111111111111111111111111111111100
111111111111111111111111111111111000
111111111111111111111111111111111111
111111111111111111111111111111111111
111111111111111111111111111111111111
111111111111111111111111111111111111
111111111111111111111111111111111111
111111111111111111111111111111111111
111111111111111111111111111111111111
111111111111111111111111111111111111
011111111111111111111111111111111111
111111111111111111111111111111111111
"

    let expectedEvent2 = hitsToCoordinates << parseStringAsMatrix <| @"
111111111111111111111111111111111111111
111111111111111111111111111111111111111
111111111111111111111111111111111111111
111111111111111111111111111111111111111
111111111111111111111111111111111111111
111111111111111111111111111111111111111
111111111111111111111111111111111111111
111111111111111111111111111111111111111
111111111111111111111111111111111111111
111111111111111111111111111111111111111
111111111111111111111111111111111111111
111111111111111111111111111111111111111
111111111111111111111111111111111111111
111111111111111111111111111111111111111
111111111111111111111111111111111111111
111111111111111111111111111111111111111
111111111111111111111111111111111111111
011111111111111111111111111111111111111
111111111111111111111111111111111111111
111111111111111111111111111111111111111
"
       


    let expectedEvent3 = hitsToCoordinates << parseStringAsMatrix <| @"
111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111100
111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111000
111111111111111111111111111111111111111111111100000011111111111111111111111111111111111111111111
"

    let expectedEvent3Alternate = hitsToCoordinates << parseStringAsMatrix <| @"
111111111111111111111
111111111111111111111
111111100000011111111
"


    let sleParams = Vertical {
        AreaThreshold = 1900<px *px>;
        MainThreshold = 20.0.percent;
        OrthogonalThreshold = 10.0.percent;
        ExtrapolateBridgeEvents = true
    }

    let expectedBounds = 
        [
            lengthsToRect 64 1 36 20;  
            lengthsToRect 4 1 39 20;
            lengthsToRect 4 9 96 3;
        ] :> seq<Rectangle<int, int>>

    let expectedAlternateBounds = lengthsToRect 43 9 21 3;

    [<Fact>]
    let ``seperate large events - testing  bounds`` () = 
        let results =  testMatrix |> getAcousticEvents  |> separateLargeEvents sleParams |> Array.ofSeq

        // expect threee events
        Assert.Equal(3, results.Length)

        // expect those three events to have the proper bounds
        Seq.iter2 (fun expected actualAcousticEvent -> Assert.Equal(expected, actualAcousticEvent.Bounds)) expectedBounds results

    [<Fact>]
    let ``seperate large events - hits returned for event 1`` () =
        let result = testMatrix |> getAcousticEvents |> separateLargeEvents sleParams |> Seq.nth 0
        
        let absoluteHits = Set.map (fun (y,x) -> (y + 1, x + 64)) expectedEvent1
        Assert.Equal<Set<_>>(absoluteHits, result.Elements)

    [<Fact>]
    let ``seperate large events - hits returned for event 2`` () =
        let result = testMatrix |> getAcousticEvents |> separateLargeEvents sleParams |> Seq.nth 1
        
        let absoluteHits = Set.map (fun (y,x) -> (y + 1, x + 4)) expectedEvent2
        Assert.Equal<Set<_>>(absoluteHits, result.Elements)

    [<Fact>]
    let ``seperate large events - hits returned for event 3`` () =
        
        let result = testMatrix |> getAcousticEvents |> separateLargeEvents sleParams |> Seq.nth 2
        
        let absoluteHits = Set.map (fun (y, x) -> (y + 9, x + 4)) expectedEvent3
        Assert.Equal<Set<_>>(absoluteHits, result.Elements)

    [<Fact>]
    let ``seperate large events - hits returned for event 3 when ExtrapolateBridgeEvents is diabled`` () =
        let sleParams = match sleParams with Vertical p -> {p with ExtrapolateBridgeEvents = false} |> Vertical
        let result = testMatrix |> getAcousticEvents |> separateLargeEvents sleParams |> Seq.nth 2
        
        let absoluteHits = Set.map (fun (y, x) -> (y + 9, x + 43)) expectedEvent3Alternate
        Assert.Equal<Set<_>>(absoluteHits, result.Elements)
        Assert.Equal(expectedAlternateBounds, result.Bounds)

// This test aims to fix a bug caused by events separation.  
module AcousticEventDetectionTestsForDebugSeperateLargeEvents =
    let testMatrix = parseStringAsMatrix @"
0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000
0000000000000000000000000000000000000000000000000000000000000000100000000000000000000000000000000000
0000000000000000000000000000000000000000000000000000000000000000100000000000000000000000000000000000
0000000000000000000000000000000000000000000000000000000000000000111000000000000000000000000000000000
0000000000000000000000000000000000000000000000000000000000000000100000000000000000000000000000000000
0000000000000000000000000000000000000000000000000000000000000000100000000000000000000000000000000000
0000000000000000000000000000000000000000000000000000000000000000100000000000000000000000000000000000
0000000000000000000000000000000000000000000000000000000000000000100000000000000000000000000000000000
0000000000000000000000000000000000000000000000000000000000000000100000000000000000000000000000000000
0000000000000000000000000000000000000000000000000000000000000000100000000000000000000000000000000000
0000111111111111111111111111111111111111111111111111111111111111100000000000000000000000000000000000
0011111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111
0011111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111
0011111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111
0011111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111
0011111111111111111111111111111111111111111000000000000000000000111111111111111111111111111111111111
0011111111111111111111111111111111111111111000000000000000000000111111111111111111111111111111111111
0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000
0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000
0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000
0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000
0000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000000
"          
    
    let expectedEvent1 = hitsToCoordinates << parseStringAsMatrix <| @"
00111111111111111111111111111111111111111111111111111111111111100000000000000000000000000000000000
11111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111
11111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111
11111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111
11111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111111
11111111111111111111111111111111111111111000000000000000000000111111111111111111111111111111111111
11111111111111111111111111111111111111111000000000000000000000111111111111111111111111111111111111
"  
    let expectedEvent2= hitsToCoordinates << parseStringAsMatrix <| @"
100
100
111
100
100
100
100
100
100
100
111
111
111
111
111
111
"
    let expectedEvent2Alternate = hitsToCoordinates << parseStringAsMatrix <| @"
100
100
111
100
100
100
100
100
100
"
    let sleParams = Horizontal {
        AreaThreshold = 1000<px *px>;
        MainThreshold = 10.0.percent;
        OrthogonalThreshold = 10.0.percent;
        ExtrapolateBridgeEvents = true
    }

    let expectedBounds = 
        [
            lengthsToRect 2 10 98 7;
            lengthsToRect 64 1 3 16;                        
        ] :> seq<Rectangle<int, int>>  
    let expectedAlternateBounds = lengthsToRect 64 1 3 9;    

    [<Fact>]
    let ``seperate large events - testing  bounds`` () = 
        let results =  testMatrix |> getAcousticEvents  |> separateLargeEvents sleParams |> Array.ofSeq

        // expect two events
        Assert.Equal(2, results.Length)

        // expect those two events to have the proper bounds
        Seq.iter2 (fun expected actualAcousticEvent -> Assert.Equal(expected, actualAcousticEvent.Bounds)) expectedBounds results

    [<Fact>]
    let ``seperate large events - hits returned for event 1`` () =
        let result = testMatrix |> getAcousticEvents |> separateLargeEvents sleParams |> Seq.nth 0
        
        let absoluteHits = Set.map (fun (y,x) -> (y + 10, x + 2)) expectedEvent1
        Assert.Equal<Set<_>>(absoluteHits, result.Elements)

    [<Fact>]
    let ``seperate large events - hits returned for event 2`` () =
        let result = testMatrix |> getAcousticEvents |> separateLargeEvents sleParams |> Seq.nth 1
        
        let absoluteHits = Set.map (fun (y,x) -> (y + 1, x + 64)) expectedEvent2
        Assert.Equal<Set<_>>(absoluteHits, result.Elements) 
    
    [<Fact>]
    let ``seperate large events - hits returned for event 3 when ExtrapolateBridgeEvents is diabled`` () =
        let sleParams = match sleParams with Horizontal p -> {p with ExtrapolateBridgeEvents = false} |> Horizontal
        let result = testMatrix |> getAcousticEvents |> separateLargeEvents sleParams |> Seq.nth 1        
        let absoluteHits = Set.map (fun (y,x) -> (y + 1, x + 64)) expectedEvent2Alternate
        Assert.Equal<Set<_>>(absoluteHits, result.Elements)
        Assert.Equal(expectedAlternateBounds, result.Bounds)

