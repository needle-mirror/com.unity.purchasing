# Unity LiveContentAdapter Client

## Generating The Client

### Prerequisites

1. If you don't already have it installed, install Java by running:
    ```bash
      brew install openjdk
    ```
2. If you don't already have it, you will need the generator jar. This can be found [here](https://pages.prd.mz.internal.unity3d.com/unity-common-openapi-generators/docs/csharp-generator/). **DO NOT SAVE IT TO VERSION CONTROL**
3. (optional) Update `./spec.yaml` from the Live Content client API spec. The gateway spec uses wildcard project hosts; replace each wildcard with an OpenAPI server variable before generation:
    ```yaml
      - url: https://{projectId}.live-content.unity3dusercontent.com/v1
        variables:
          projectId:
            default: project-id
    ```
    Apply the same conversion to the `live-content-stg.unity3dusercontent.com` server.

### Option A) Using the Script (macOS, Linux, or Windows)

#### Prerequisites
- A Unity 6000+ editor installed via Unity Hub
- A POSIX shell — native on macOS/Linux; on Windows use Git Bash or WSL

Run `script-regenerate-code.sh`, passing the path to the generator jar:

```bash
  bash ./script-regenerate-code.sh ./path/to/generator.jar
```

This will automatically:
- Generate the client to `./tmp-client`, delete `./Client`, and replace it with the generated output
- Generate `.meta` files using the Unity editor. The script picks the latest Unity 6000+ editor from the Hub default location for the detected OS:
  - **macOS**: `/Applications/Unity/Hub/Editor/`
  - **Windows (Git Bash)**: `C:\Program Files\Unity\Hub\Editor\`
  - **Linux**: `~/Unity/Hub/Editor/`
  - **WSL** (no native Linux Hub): `/mnt/c/Program Files/Unity/Hub/Editor/`
  - Set `UNITY_PATH` to override the auto-detected binary (e.g. `UNITY_PATH=/path/to/Unity bash ./script-regenerate-code.sh ...`)
- Restore any pre-existing `.meta` files from version control
- Wrap unconditional `AddParamsToQueryParams` calls with `string.IsNullOrEmpty` guards so null/empty query params are omitted from the request URL

### Option B) Manual Steps

1. Run the generator with the configuration file `./configuration.yaml`:
    ```bash
      java -jar ./path/to/generator.jar generate -c ./configuration.yaml
    ```
2. Extract and move the generated client to `./Client`:
    ```bash
      rm -rf ./Client && mkdir ./Client && mv ./tmp-client/com.unity.purchasing.liveContentAdapter/Runtime/* ./Client/ && rm -rf ./tmp-client
    ```
3. Open the Test Project in Unity to generate `.meta` files for any new files.
4. Restore pre-existing `.meta` files from version control:
    ```bash
      git checkout -- Client/**/*.meta Client/*.meta
    ```
5. Run `./script-fixup-generated-code.sh` to wrap unconditional `AddParamsToQueryParams` calls with `string.IsNullOrEmpty` guards.


### Additional Manual Steps

1. Verify that generated request paths do not contain `/projects/{projectId}`. The project ID is part of the base URL subdomain, which is initialized by `IapCoreInitializeCallback`.
2. Review `spec.yaml` and generated code for unexpected changes. The public API spec is manually maintained and may contain errors. Raise potential discrepancies with the team.
3. Make sure to add any new, untracked files, especially new .meta files.
