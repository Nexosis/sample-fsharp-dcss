# Analyzing Tournament impact of Dungeon Crawl Stone Soup

This project is a sample application of the [Nexosis Api](http://nexosis.com/).  It illustrates how to use the Api to generate [impact analysis](http://docs.nexosis.com/guides/impactanalysis) of several date ranges in a dataset.

## Setup

In order to fully use this sample project and generate the Html page output, you will need to follow a few basic steps to run this using your own Nexosis account.

- Create a new file in this directory called `api-key.secret`.  Save [your Api key](https://developers.nexosis.com/developer) in this file.
- Run `./build upload` - Downloads the current data from the DCSS crawl server and upload it as a new dataset named `DCSS` in your Nexosis Api account.
- Run `./build impact` - Starts a new set of impact sessions on this dataset.  It will prompt with an estimate before starting the sessions.
- Run `./build run` - This will first check to ensure that all of the sessions which were started have completed.  This may take some time for all sessions to complete running.
- Output can also be generated with `./build html`, which will create an Html page in the `output` folder.

This does not run impact sessions on all tournaments by default.  If you would like to change this, the `NexosisApiIntegration.fsx` file contains a list that can be modified.
