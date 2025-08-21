# credits: https://www.jokecamp.com/blog/powershell-script-dynamically-set-dotnet-assebmly-versions/

function getVersion()
{
    $tag = iex "git describe --long --tags --always"
    $pattern = [regex]"v?\d+\.\d+\.\d+\-\d+"
    $match = $pattern.Match($tag)

    if (-not $match.Success) {
        Write-Error "Version does not match pattern 'x.x.x-x'. Found: $tag"
        exit 1
    }

    $version = $match.Captures[0].value
    $version = $version -replace '-', '.'
    $version = $version -replace 'v', ''
    Write-Host "Version found: $version"
    return $version
}

function SetVersion ($file, $version)
{
    Write-Host "Changing version in $file to $version"
    $content = Get-Content -Path $file -Raw -Encoding UTF8
    $content = $content -replace "(\d+)\.(\d+)\.(\d+)[\.(\d+)]*", $version
    Set-Content -Path $file -Value $content -Encoding UTF8 -NoNewline
}

function setVersionInDir($projectNamespace, $dir, $fileName, $version) {

    if ($version -eq "") {
        Write-Host "version not found"
        exit 1
    }

    # Set the Assembly version
    $info_files = Get-ChildItem $dir -Recurse -Include $fileName | where {$_ -match $projectNamespace}
	
    foreach($file in $info_files)
    {
        Setversion $file $version
    }
}

$projectNamespace=$args[0]
$solutionDir=$args[1]
$fileName=$args[2]
$version = getVersion
setVersionInDir $projectNamespace $solutionDir $fileName $version
