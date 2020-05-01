module GeneralUtils
open System.Threading

let padDelay (num:int) fn = 
    Thread.Sleep(num)
    fn ()
    Thread.Sleep(num)

