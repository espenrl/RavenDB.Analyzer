# RavenDB code analyzer

Solution created from Roslyn Analyzer template. `DiagnosticVerifier.Helper.cs` modified to account for Raven dll references in unit tests.

NuGet package `Microsoft.CodeAnalysis.CSharp.Workspace` is set to v1.3.2 and should not be upgraded. This is tied to Visual Studio versions and ensures compatibility with Visual Studio 2015.

NuGet package `RavenDB.Client` should be v3.0.x as the analyzer is designed for use with RavenDB 3.0. This may not play an important role as only the test project needs to reference RavenDB assemblies.

### Project 'RavenDB.Analyzer.Vsix'

Used for debugging the analyzers. Hit F5 and go. Another instance of Visual Studio (an experimental instance) opens with analyzer loaded.

In new instance open any solution/project and debug the analyzers.