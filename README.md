

## Requirements

- MSVC for AssImp in Olve.Trains.AssetPipeline
- bun for api gen



## FAQ

```
C:\Users\olive\.nuget\packages\microsoft.extensions.apidescription.server\9.0.8\build\Microsoft.Extensions.ApiDescription.Server.targets(68,5): error : System.AggregateException: One or more errors occurred. (Could not load the embedded file manifest 'Microsoft.Extensions.FileProviders.Embedded.Manifest.xml' for assembly 'Olve.Engine3D.DebugServer'.)
```

apigen + building the debug server kind of blocks eachother.

The solution is to create a SvelteDist directory in Olve.Engine3D.DebugServer and then put some file (e.g. `index.html`) inside.

This will allow for building Olve.Engine3D.DebugServer.Host which is required to run api generation.