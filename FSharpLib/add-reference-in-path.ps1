param (
    [string]$projectFile,
    [string]$dllDirectory
)

# Get the full path to the project file and DLL directory
$projectFile = Resolve-Path $projectFile
$dllDirectory = Resolve-Path $dllDirectory

# Add references to all DLLs in the given directory
Get-ChildItem -Path $dllDirectory -Filter *.dll | ForEach-Object {
    dotnet add "$projectFile" reference $_.FullName
}