# RavenDB v3.0 code analyzer

### Usage

```
Install-Package erl.RavenDB.Analyzer
```

Link to [NuGet package](https://www.nuget.org/packages/erl.RavenDB.Analyzer).

### Developer notes

Solution created from Roslyn analyzer template. `DiagnosticVerifier.Helper.cs` was modified to account for Raven dll references in unit tests.

NuGet package `Microsoft.CodeAnalysis.CSharp.Workspace` is set to v1.3.2 and should not be upgraded. This is tied to Visual Studio versions and ensures compatibility with Visual Studio 2015.

NuGet package `RavenDB.Client` should be v3.0.x as the analyzer is designed for use with RavenDB v3.0. This may not play an important role as only the test project needs to reference RavenDB assemblies. The diagnostics is tied to type name and not assemblies in RavenDB.

### Project RavenDB.Analyzer.Vsix

Used for debugging the analyzers. Hit F5 and go. Another instance of Visual Studio (an experimental instance) opens with analyzer loaded.

In new instance open any solution/project and debug the analyzers.

NOTE: In this mode the analyzer is not applied in the build process. It is however applied in the editor and will provoke diagnostics on the source code.