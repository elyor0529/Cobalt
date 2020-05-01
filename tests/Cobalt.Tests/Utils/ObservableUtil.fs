module ObservableUtil
open System

type MonitorResults<'a> () = 
    member val values = List.empty with get, set
    member val exns = List.empty with get, set
    member val completed = false with get, set

    member x.noExns = List.length x.exns = 0
    member x.noValues = List.length x.values = 0

    member x.isJustOneValue =
        if x.noExns && not x.completed && List.length x.values <> 1 then
            None
        else Some(List.exactlyOne x.values)

    member x.isNothing = x.noExns && not x.completed && List.length x.values = 0

    interface IObserver<'a> with
        member x.OnNext v =
            x.values <- v :: x.values
        member x.OnError e =
            x.exns <- e :: x.exns
        member x.OnCompleted () = 
            x.completed <- true

            

let monitor<'a> (obs:IObservable<'a>) fn =
    let results = MonitorResults()
    use __ = obs.Subscribe(results)
    fn ()
    results

