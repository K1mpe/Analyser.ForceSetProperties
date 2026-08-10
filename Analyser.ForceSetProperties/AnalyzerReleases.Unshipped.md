; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
FSP001 | ForceSetProperties | Error | Missing property assignment
FSP002 | ForceSetProperties | Error | Multiple properties missing
FSP003 | ForceSetProperties | Error | Unsupported attribute target
FSP004 | ForceSetProperties | Error | Unsupported destination type
FSP005 | ForceSetProperties | Error | No properties found to validate
FSP006 | ForceSetProperties | Error | Ambiguous inferred generic destination type
FSP007 | ForceSetProperties | Error | Conflicting destination type specification
FSP101 | ForceSetProperties | Warning | Destination type is an interface
FSP201 | ForceSetProperties | Info | All properties validated
