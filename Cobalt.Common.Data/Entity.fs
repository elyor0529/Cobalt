namespace Cobalt.Common.Data

type Entity() =
    let mutable _id = null
    member this.Id
        with get() = _id
        and set(id: obj) = _id <- id

type Entity<'a>() = 
    inherit Entity()
    member this.Id
        with get() =  base.Id :?> 'a
        and set(id: 'a) = base.Id <- id

