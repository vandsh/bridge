# -|--|- Bridge

## The what? 
 - A Yaml based service for handling the promotion of Kentico CMS based items thru source control.

## The why? 
 - Because XML is terrible to read and even harder to merge. The reason I created this was to allow us to not only break down features modularly thru some basic configs, but I wanted to make sure it was easy to adopt and that means making it easy to use with source control.

## The when? 
 - Use this either along side an existing Kentico build using the [nuget](https://www.nuget.org/packages/Bridge.Kentico/) or standalone by cloning this repo and updating the `connectionString` parameters to match your Kentico db.

## The how?
 - Installation:
     - *Standalone*: clone, compile and run the app. Fire up a browser and go to `<localhost>/bridge/index`
     - *Alongside*: install via [nuget](https://www.nuget.org/packages/Bridge.Kentico/) and navigate to `<kentico url>/Admin/BridgeUI/index`
 - The configs:
     - *CoreConfig*: one or more core config can be specified. They contain a list of `classtypes` to care about and a list of `ignoreFields` on the item to simply ignore when serializing/syncing.
     - *ContentConfig*: one or more content config can be specified. Contains a list of `pagetypes` to care about and a list of `ignoreFields` on the items to not serialize/sync. This also will handle all custom fields. Also, a `query` attribute allows you to pick which content within the tree to focus on. This should support a basic Kentico Query.
 - Using the GUI:
     - Core Configs:
        - Core Configs *Serialize All*: iterates thru all of the defined `coreConfig` nodes and fetches all the corresponding classes, and stuffs them into the `serialization/core/<coreConfigName>` folder.
        - Core Configs *Sync All*: this is the inverse of the `Serialize All` task, it takes what's in the file system and pushes it to the Kentico DB.
        - "Named Core Config" *Serialize*: pulls _just_ the specified config down to the file system
        - "Named Core Config" *Sync*: pushes _just_ the specified config up to the database
        - "Named Core Config" *Diff*: does a temp serialize of the current Kentico DB and compares it to what's in the filesystem and spits out a diff.
    - Content Configs:
        - Content Configs *Serialize All*: iterates thru all of the defined `contentConfig` nodes and fetches all the corresponding classes, and stuffs them into the `serialization/content/<contentConfigName>` folder.
        - Content Configs *Sync All*: this is the inverse of the `Serialize All` task, it takes what's in the file system and pushes it to the Kentico DB.
        - "Named Content Config" *Serialize*: pulls _just_ the specified config down to the file system
        - "Named Content Config" *Sync*: pushes _just_ the specified config up to the database
        - "Named Content Config" *Diff*: does a temp serialize of the current Kentico DB and compares it to what's in the filesystem and spits out a diff.
 - The Endpoints:
     - Within a dev-ops cycle (ie a release to a dev or test environment) make a call to the Bridge endpoints using an authenticated call (using something like `curl`, `wget` or `Invoke-WebRequest`).
     - The response itself should be a stream so it will respond as things occur rather than processing everything and responding.
     - The endpoints are as follows:
         - `<your url>/bridge/diffcore?/{configName}`: returns the comparison between the serialized DB and what is in the file system for the optional core configName
         - `<your url>/bridge/diffcontent?/{configName}`: returns the comparison between the serialized DB and what is in the file system for the optional content configName
         - `<your url>/bridge/serializecore?/{configName}`: serializes the specified core config (or all if none specified) to the filesystem
         - `<your url>/bridge/serializecontent?/{configName}`: serializes the specified content config (or all if none specified) to the filesystem
         - `<your url>/bridge/synccore?/{configName}`: syncs the specified core config (or all if none specified) to the database
         - `<your url>/bridge/synccontent?/{configName}`: syncs the specified content config (or all if none specified) to the database
     - *My ultimate goal is to leverage the endpoints in order to promote a continuous integration and deployment methodology within the Kentico community*

