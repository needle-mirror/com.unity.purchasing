### Initialize Unity Gaming Services
Call `UnityServices.InitializeAsync()` to initialize all Unity Gaming Services at once.
It returns a `Task` that enables you to monitor the initialization's progression.

#### Example
```cs
using System;
using Unity.Services.Core;
using Unity.Services.Core.Environments;
using UnityEngine;

public class InitializeUnityServices : MonoBehaviour
{
    public string environment = "production";

    async void Start()
    {
        try
        {
            var options = new InitializationOptions()
                .SetEnvironmentName(environment);

            await UnityServices.InitializeAsync(options);
        }
        catch (Exception exception)
        {
            // An error occured during services initialization.
        }
    }
}
```

## Technical details

The `InitializeAsync` methods affect the currently installed service packages in your Unity project.

Note that this method is not supported during edit time.
