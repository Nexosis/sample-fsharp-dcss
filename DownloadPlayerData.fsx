(*** hide ***)
#load "packages/FsLab/Themes/DefaultWhite.fsx"
#load "packages/FsLab/FsLab.fsx"

open FSharp.Data
open Deedle

(**

# Downaloding data from the Crawl stats page

Since this data isn't presented in an easily useable format, we need to a bit of work in order to download all of the data, and then do a small bit of processing.  We can accomplish this pretty easily using the `HtmlProvider`.

We need to setup the provider.  The `Server Activity.html` file is the downloaded stats page, with most of the rows deleted.  This is just so that the Type Provider is able to more quickly parse the page without having to scan the entire table.
*)

let [<Literal>]SampleFile = __SOURCE_DIRECTORY__ + "/Server Activity.html"
let [<Literal>]CrawlStatsUrl = "http://crawl.akrasiac.org/scoring/per-day.html"

type StatsPage = HtmlProvider<SampleFile>

let pageHtml =
    StatsPage.Load(CrawlStatsUrl)

(**
Now, lets define a type that we can use to represent this data.  We are just going to look at the counts for each day.  The names of all of the winners are also included in the html, but, we don't need to carry these forwards.
*)

type DailyStat = {
    Date:       System.DateTime
    Games:      int
    Players:    int
    Wins:       int
}

(**
Also, we only want to look at data from 2012 onwards, since that is the last 10 tournaments.
*)

let cutoffDate = System.DateTime(2012, 01, 01)

(**
And, this is all we need to create the dataset we will work with later.  The `HtmlProvider` does most of the hard work by finding the table that we want to pull data from and exposing the rows.  It is also able to figure out the correct datatype for all of the values as well.  The `distinctBy` call is to remove some of the rows that are monthy rollup summaries.  We don't want these computed summaries in our dataset.  Since the table creates these summaries with the date timestamp in the format `2017-06`, the type provider will parse this as `2017-06-01`.  Since there are then two rows with the same date, we can use `distictBy` to remove the second row, which happens to be the summary we don't want.  The call to tail is to remove todays data, since that is an incomplete record.  We then apply the filter to remove the older data that we won't be using, and map the data into our type, and finally pipe everything into a DataFrame that will be used later.
*)

let dataSet =
    pageHtml.Tables.``Server Activity``.Rows
    |> Array.distinctBy (fun r -> r.Date)
    |> Array.tail
    |> Array.filter (fun r -> r.Date >= cutoffDate)
    |> Array.map (fun r -> { Date = r.Date; Games = r.Games; Players = r.Players; Wins = r.Wins })
    |> Frame.ofRecords
    |> Frame.indexRowsDate "Date"
    |> Frame.sortRowsByKey

(*** include-value:dataSet ***)

(**
Now, we have a fairly large dataset to work with.  We can now jump back to the main story.
*)

(*** define-output:rowcount ***)
printfn "total rowcount is %d" (dataSet |> Frame.countRows)
(*** include-output:rowcount ***)


(**
We can also pull out series of each value for use later on
*)
let observedGames = dataSet?Games
let observedPlayers = dataSet?Players
let observedWins = dataSet?Wins
