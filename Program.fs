module Program

open Hopac
open System
open System.Reactive.Linq
open System.Reactive.Disposables
open System.Threading

module Job = 
    open System.Reactive.Subjects

    let toObserver (j: Job<'a>) = 
        let hookUp (prom: Promise<'a>) (subject: IObserver<'a>) = 
            job {
              let! res = prom |> Job.catch
              match res with
              | Choice1Of2 ok -> 
                subject.OnNext ok
                subject.OnCompleted ()
              | Choice2Of2 err -> 
                subject.OnError err
            }
        
        let startOnBackground j sub =
            job {
                let! prom = Promise.start j
                do! hookUp prom sub
            } |> Hopac.start
        
        let subject = new AsyncSubject<_> ()
        startOnBackground j subject
        
        subject.AsObservable()

let inline choiceToObs (obs : IObserver<_>) value =
    match value with
    | Choice1Of2 x -> x |> obs.OnNext; obs.OnCompleted()
    | Choice2Of2 e -> e |> obs.OnError


let myFromJob computation = 
    Job.toObserver computation 

let fromJob computation =
    Observable.Defer(Func<_>(fun () ->
        { new IObservable<'a> with
            member __.Subscribe obs =
                computation
                |> Job.catch
                |> Job.map (choiceToObs obs)
                |> Hopac.start
                { new IDisposable with
                    member __.Dispose() = 
                        printfn "disposed!" } }))


let inline subscribeJobNoConcat (onNextJob: _ -> Job<unit>) (obs: IObservable<_>) = 
    obs
      .Select (fun item -> 
        onNextJob item
        |> Job.catch
        |> Job.map (function | Choice1Of2 x -> x | Choice2Of2 err -> raise err)
        |> Hopac.start
      )

let inline subscribeJob (onNextJob : _ -> Job<unit>) (obs : IObservable<_>) =
   obs
    .Select(onNextJob >> fromJob)
    .Concat()

let inline subscribeJobMyFromJob (onNextJob : _ -> Job<unit>) (obs : IObservable<_>) =
   obs
    .Select(onNextJob >> myFromJob)
    .Concat()


let generateData () =
    let generator (obs : IObserver<string>) (ct : CancellationToken) = 
        job {
            let flag = new BooleanDisposable()
            let mutable counter = 1
            while true do
                printfn "writing %s" "hello"
                do! timeOutMillis 100
                obs.OnNext(sprintf "hello %d" counter)
                counter <- counter + 1
            return flag :> IDisposable
        } |> startAsTask
    Observable.Create(generator)


[<EntryPoint>]
let main argv =

    use foo = 
        generateData ()
        |> subscribeJobMyFromJob (fun nextVal -> job {
            printfn "wrote %s" nextVal
            do! timeOutMillis 10
        })
        |> fun s -> s.Subscribe()
    Console.ReadLine() |> ignore
    0