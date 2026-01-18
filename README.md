# Inspection Processor (Azure Functions)

## Prerequisites
- .NET SDK 8.x
- Azure Functions Core Tools (v4)

## Restore
```powershell
dotnet restore
```

## Build
```powershell
dotnet build
```

## Run locally
```powershell
func start
```

If your repo lives in a path with spaces (like OneDrive), Core Tools can fail due to an unquoted output path. Use one of these workarounds:

Option 1: Build with `dotnet`, then run without building
```powershell
dotnet build
func start --no-build
```

Option 2: Map a drive letter to avoid spaces
```powershell
subst X: "C:\Users\JasonCavaliere\OneDrive - Frontier Energy, Inc\Desktop\daccess\GitHub\inspection-processor-poc"
X:
func start
```
Remove the mapping when done:
```powershell
subst /d X:
```
