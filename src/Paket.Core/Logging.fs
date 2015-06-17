﻿module Paket.Logging

open System
open System.IO
open System.Diagnostics

/// [omit]
let mutable verbose = false

/// [omit]
type Trace = {
    Level: TraceLevel
    Text: string
    NewLine: bool }

/// [omit]
let event = Event<Trace>()

/// [omit]
let subscribe callback = Observable.subscribe callback event.Publish

/// [omit]
let subscribeOn ctx callback = Observable.subscribeOn ctx callback event.Publish

/// [omit]
let tracen s = event.Trigger { Level = TraceLevel.Info; Text = s; NewLine = true }

/// [omit]
let tracefn fmt = Printf.ksprintf tracen fmt

/// [omit]
let trace s = event.Trigger { Level = TraceLevel.Info; Text = s; NewLine = false }

/// [omit]
let tracef fmt = Printf.ksprintf trace fmt

/// [omit]
let traceVerbose s =
    if verbose then
        event.Trigger { Level = TraceLevel.Verbose; Text = s; NewLine = true }

/// [omit]
let verbosefn fmt = Printf.ksprintf traceVerbose fmt

/// [omit]
let traceError s = event.Trigger { Level = TraceLevel.Error; Text = s; NewLine = true }

/// [omit]
let traceWarn s = event.Trigger { Level = TraceLevel.Warning; Text = s; NewLine = true }

/// [omit]
let traceErrorfn fmt = Printf.ksprintf traceError fmt

/// [omit]
let traceWarnfn fmt = Printf.ksprintf traceWarn fmt


// Console Trace

/// [omit]
let traceColored color (s:string) = 
    let curColor = Console.ForegroundColor
    if curColor <> color then Console.ForegroundColor <- color
    use textWriter = 
        match color with
        | ConsoleColor.Red -> Console.Error
        | _ -> Console.Out
    textWriter.WriteLine s
    if curColor <> color then Console.ForegroundColor <- curColor

/// [omit]
let monitor = new Object()

/// [omit]
let traceToConsole (trace:Trace) =
    lock monitor
        (fun () ->
            match trace.Level with
            | TraceLevel.Warning -> traceColored ConsoleColor.Yellow trace.Text
            | TraceLevel.Error -> traceColored ConsoleColor.Red trace.Text
            | _ ->
                if trace.NewLine then Console.WriteLine trace.Text
                else Console.Write trace.Text )


// Log File Trace

/// [omit]
let mutable logFile : string option = None

/// [omit]
let traceToFile (trace:Trace) =
    match logFile with
    | Some fileName -> try File.AppendAllLines(fileName,[trace.Text]) with | _ -> ()
    | _ -> ()

/// [omit]
let setLogFile fileName =
    let fi = FileInfo fileName
    logFile <- Some fi.FullName
    if fi.Exists then
        fi.Delete()
    else
        if fi.Directory.Exists |> not then
            fi.Directory.Create()
    subscribe traceToFile