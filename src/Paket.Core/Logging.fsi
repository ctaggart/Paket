﻿module Paket.Logging

open System
open System.Diagnostics
open System.Threading

val mutable verbose : bool


val tracen : string -> unit

val tracefn : Printf.StringFormat<'a,unit> -> 'a

val trace : string -> unit

val tracef : Printf.StringFormat<'a,unit> -> 'a

val traceVerbose : string -> unit

val verbosefn : Printf.StringFormat<'a,unit> -> 'a

val traceError : string -> unit

val traceWarn : string -> unit

val traceErrorfn : Printf.StringFormat<'a,unit> -> 'a

val traceWarnfn : Printf.StringFormat<'a,unit> -> 'a


type Trace = {
    Level: TraceLevel
    Text: string
    NewLine: bool }

val event : Event<Trace>

val subscribe : (Trace -> unit) -> IDisposable

val subscribeOn : QueuingSynchronizationContext -> (Trace -> unit) -> IDisposable

val traceToConsole : Trace -> unit

val setLogFile : string -> IDisposable