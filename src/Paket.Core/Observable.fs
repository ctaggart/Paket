// adapted from MiniRx
// http://minirx.codeplex.com/
[<AutoOpen>]
module Paket.ObservableExtensions

open System
open System.Threading

let private synchronize f = 
    let ctx = System.Threading.SynchronizationContext.Current 
    f (fun g arg ->
        let nctx = System.Threading.SynchronizationContext.Current 
        if ctx <> null && ctx <> nctx then 
            ctx.Post((fun _ -> g(arg)), null)
        else 
            g(arg))

type Microsoft.FSharp.Control.Async with 
    static member AwaitObservable(ev1:IObservable<'a>) =
        synchronize (fun f ->
            Async.FromContinuations((fun (cont,econt,ccont) -> 
            let rec callback = (fun value ->
                remover.Dispose()
                f cont value )
            and remover : IDisposable  = ev1.Subscribe(callback) 
            () )))

type IObservable<'T> with
    /// Subscribes on a specific SynchronizationContext. All notifications are sent using the context.
    member source.SubscribeOn context = 
        assert (context <> null)
        {
            new IObservable<_> with
                member __.Subscribe observer = 
                    let invoke f = 
                        if SynchronizationContext.Current = context 
                        then f() 
//                        else context.Send(SendOrPostCallback(fun _ -> f()), null)
                        else context.Post(SendOrPostCallback(fun _ -> f()), null)
                    source.Subscribe {
                        new IObserver<_> with 
                            member __.OnNext value = invoke <| fun() -> observer.OnNext value
                            member __.OnError error = invoke <| fun() -> observer.OnError error
                            member __.OnCompleted() = invoke observer.OnCompleted
                    }
        }

[<RequireQualifiedAccess>]
module Observable =
    open System.Collections.Generic

    /// Creates an observable that calls the specified function after someone
    /// subscribes to it (useful for waiting using 'let!' when we need to start
    /// operation after 'let!' attaches handler)
    let guard f (e:IObservable<'Args>) =  
        { new IObservable<'Args> with  
            member x.Subscribe(observer) =  
                let rm = e.Subscribe(observer) in f(); rm } 

    let sample milliseconds source =
        let relay (observer:IObserver<'T>) =
            let rec loop () = async {
                let! value = Async.AwaitObservable source
                observer.OnNext value
                do! Async.Sleep milliseconds
                return! loop() 
            }
            loop ()

        { new IObservable<'T> with
            member this.Subscribe(observer:IObserver<'T>) =
                let cts = new System.Threading.CancellationTokenSource()
                Async.Start(relay observer, cts.Token)
                { new IDisposable with 
                    member this.Dispose() = cts.Cancel() 
                }
        }

    let ofSeq s = 
        let evt = new Event<_>()
        evt.Publish |> guard (fun o ->
            for n in s do evt.Trigger(n))

    let private oneAndDone (obs : IObserver<_>) value =
        obs.OnNext value
        obs.OnCompleted() 

    let ofAsync a : IObservable<'a> = 
        { new IObservable<'a> with
                member __.Subscribe obs = 
                    let oneAndDone' = oneAndDone obs
                    let token = new CancellationTokenSource()
                    Async.StartWithContinuations(a,oneAndDone',obs.OnError,obs.OnError,token.Token)
                    { new IDisposable with
                        member __.Dispose() = 
                            token.Cancel |> ignore
                            token.Dispose() } }
        
    let ofAsyncWithToken (token : CancellationToken) a : IObservable<'a> = 
        { new IObservable<'a> with
                member __.Subscribe obs = 
                    let oneAndDone' = oneAndDone obs
                    Async.StartWithContinuations(a,oneAndDone',obs.OnError,obs.OnError,token)
                    { new IDisposable with
                        member __.Dispose() = () } }

    let flatten (a: IObservable<#seq<'a>>): IObservable<'a> =
        { new IObservable<'a> with
            member __.Subscribe obs =
                let sub = a |> Observable.subscribe (Seq.iter obs.OnNext)
                { new IDisposable with member __.Dispose() = sub.Dispose() }}

    let distinct (a: IObservable<'a>): IObservable<'a> =
        let seen = HashSet()
        Observable.filter seen.Add a
 
    /// Subscribes on a specific SynchronizationContext. All notifications are sent using the context.
    let subscribeOn context (callback:'T -> unit) (source:IObservable<'T>) = 
        Observable.subscribe callback (source.SubscribeOn context)