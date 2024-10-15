$solutionDir=$args[0]
$reportPath=$args[1]

Write-Host "LibraryDependencyReport for solution" $solutionDir "will be generated to" $reportPath"."
$processOptions = @{
    FilePath = "dotnet"
    RedirectStandardOutput = $reportPath
}

Start-Process @processOptions -Wait "list $solutionDir package --format json"

$pathPattern = $solutionDir.Replace("\","/")
$content = [System.IO.File]::ReadAllText($reportPath).Replace($pathPattern,"<solutionDir>/")
$result = [System.IO.File]::WriteAllText($reportPath, $content)

exit $LASTEXITCODE