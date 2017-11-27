
#load "packages/SwaggerProvider/SwaggerProvider.fsx"
#load "./DownloadPlayerData.fsx"

open System
open System.IO
open SwaggerProvider
open Deedle
open DownloadPlayerData
open FSharp.Data

let latestTournamentImpactSessionFile =  __SOURCE_DIRECTORY__ + "/Sessions/latestTournamentImpactSession.txt"
let impactSessionsFile = __SOURCE_DIRECTORY__ + "/Sessions/impactSessions.txt"
let apiKey =
    let keyFilePath = __SOURCE_DIRECTORY__ + "/api-key.secret"
    try
        File.ReadAllText(keyFilePath)
    with
        | _ -> failwith (sprintf "Api key is not found.  Save your Nexosis API key to %s.  You can find your API key here: https://developers.nexosis.com/developer" keyFilePath)

let loadLatestWinsImpactSessionId = 
    try
        Some(File.ReadAllText latestTournamentImpactSessionFile)
    with
        | _ -> None

let impactSessionIds =
    try
        Some(File.ReadAllLines impactSessionsFile)
    with
        | _ -> None

let allRequestSessionIds = 
        match impactSessionIds with
        | Some sessions -> sessions |> List.ofArray
        | None -> List.empty

let latestWinsImpactSessionId =
    match loadLatestWinsImpactSessionId with
    | Some session -> session
    | None -> ""
    

let saveSessionIds fileName sessionIds =
    File.WriteAllLines(fileName, (Seq.toArray sessionIds))

let [<Literal>]Schema = "https://developers.nexosis.com/docs/services/98847a3fbbe64f73aa959d3cededb3af/export?DocumentFormat=Swagger"
let [<Literal>]Url = "https://ml.nexosis.com"

(*** define:swaggerSetup ***)
let addAPIKeyHeader (request:Net.HttpWebRequest) =
    request.Headers.Add("api-key", apiKey)
    request

type Nexosis = SwaggerProvider<Schema>

let nexosis = Nexosis(Url)
nexosis.CustomizeHttpRequest <- addAPIKeyHeader

(** - *)

let uploadData =
    let data = Nexosis.Data()
    data.Data <- dataSet.Rows
        |> Series.observations
        |> Seq.map (fun (date,r) ->
                        let frameRow = r.As<string>()
                        (Seq.append [("timestamp", date.ToString("o"))]
                            (frameRow |> Series.observations)) |> Map.ofSeq) |> Seq.toArray
    data

let uploadPlayerData() =
    nexosis.DatasetsAddData("DCSS", uploadData) |> ignore
    printfn "Uploaded %i rows of data successfully" uploadData.Data.Length

let dataAlreadyUploaded() = 
    let dataSets = nexosis.DatasetsListAll("DCSS", None, None)
    dataSets.Items.Length <> 0

let getCompleteSessions() =
    allRequestSessionIds 
    |> List.map nexosis.SessionsRetrieveSession
    |> List.filter (fun s -> s.Status = "completed")

let downloadResults() = 
    allRequestSessionIds
    |> List.map (fun s -> nexosis.SessionsRetrieveResults(s, "0.5"))
    |> List.sortBy (fun r -> r.ExtraParameters.["event"])

type Tournament = {
    Name:   string
    Start:  DateTime
    End:    DateTime
}

//Uncomment if you would like to run impact sessions on all of the listed tournaments.
let allTournaments = 
    [   
        // { Name = "0.10"; Start = DateTime(2012, 02, 25); End = DateTime(2012, 03, 11) }
        // { Name = "0.11"; Start = DateTime(2012, 10, 20); End = DateTime(2012, 11, 04) }
        // { Name = "0.12"; Start = DateTime(2013, 05, 11); End = DateTime(2013, 05, 26) }
        // { Name = "0.13"; Start = DateTime(2013, 10, 11); End = DateTime(2013, 10, 27) }
        // { Name = "0.14"; Start = DateTime(2014, 04, 11); End = DateTime(2014, 04, 27) }
        // { Name = "0.15"; Start = DateTime(2014, 08, 29); End = DateTime(2014, 09, 14) }
        // { Name = "0.16"; Start = DateTime(2015, 03, 13); End = DateTime(2015, 03, 29) }
        { Name = "0.17"; Start = DateTime(2015, 11, 06); End = DateTime(2015, 11, 22) }
        { Name = "0.18"; Start = DateTime(2016, 05, 06); End = DateTime(2016, 05, 22) }
        { Name = "0.19"; Start = DateTime(2016, 11, 04); End = DateTime(2016, 11, 20) }
        { Name = "0.20"; Start = DateTime(2017, 05, 26); End = DateTime(2017, 06, 11) }
    ]

let allRequests = 
    allTournaments 
        |> Seq.collect (fun tournament ->
            dataSet.Columns.Keys
                |> Seq.map (fun columnName ->
                    let impactRequest = Nexosis.ImpactSessionData()
                    impactRequest.DataSourceName <- "DCSS"
                    impactRequest.TargetColumn <- columnName
                    impactRequest.EventName <- tournament.Name
                    impactRequest.StartDate <- Some(tournament.Start)
                    impactRequest.EndDate <- Some(tournament.End)
                    nexosis.SessionsCreateImpactSession(impactRequest)
            )
        )

let columnsByTournament =
    allTournaments
    |> List.collect (fun tournament ->
        dataSet.Columns.Keys |> Seq.map (fun columnName ->
            (columnName, tournament)
        ) |> Seq.toList
    )

let startSessions sessionParams =
    let sessions =
        sessionParams
        |> List.map (fun (targetColumn, tournament) ->
                    let impactRequest = Nexosis.ImpactSessionData()
                    impactRequest.DataSourceName <- "DCSS"
                    impactRequest.TargetColumn <- targetColumn
                    impactRequest.EventName <- tournament.Name
                    impactRequest.StartDate <- Some(tournament.Start)
                    impactRequest.EndDate <- Some(tournament.End)
                    nexosis.SessionsCreateImpactSession(impactRequest)
        )

    sessions 
        |> List.filter (fun s -> s.ExtraParameters.["event"] = "0.20" && s.TargetColumn = "Wins")
        |> List.map (fun s -> s.SessionId)
        |> saveSessionIds latestTournamentImpactSessionFile

    sessions 
        |> List.map (fun s -> s.SessionId)
        |> saveSessionIds impactSessionsFile

let downloadOldSessionsAndSaveToFile() =
    let sessions = nexosis.SessionsListAll("DCSS", null, null, null, null, None, None)
    sessions.Items 
        |> Seq.filter (fun s -> s.Status = "completed")
        |> Seq.distinctBy (fun s -> (s.ExtraParameters.["event"], s.TargetColumn))
        |> Seq.map (fun s -> s.SessionId)
        |> saveSessionIds impactSessionsFile
        |> ignore
