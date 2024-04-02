# credits: https://www.jokecamp.com/blog/powershell-script-dynamically-set-dotnet-assebmly-versions/

function getVersion()
{
    $tag = iex "git describe --long --tags --always"
    $a = [regex]"v\d+\.\d+\.\d+\-\d+"
    $b = $a.Match($tag)
    $b = $b.Captures[0].value
    $b = $b -replace '-', '.'
    $b = $b -replace 'v', ''
    Write-Host "Version found: $b"
    return $b
}

function SetVersion ($file, $version)
{
    Write-Host "Changing version in $file to $version"
    $fileObject = get-item $file

    $sr = new-object System.IO.StreamReader( $file, [System.Text.Encoding]::GetEncoding("utf-8") )
    $content = $sr.ReadToEnd()
    $sr.Close()

    $content = [Regex]::Replace($content, "(\d+)\.(\d+)\.(\d+)[\.(\d+)]*", $version);

    $sw = new-object System.IO.StreamWriter( $file, $false, [System.Text.Encoding]::GetEncoding("utf-8") )
    $sw.Write( $content )
    $sw.Close()
}

function setVersionInDir($projectNamespace, $dir, $version) {

    if ($version -eq "") {
        Write-Host "version not found"
        exit 1
    }

    # Set the Assembly version
    $info_files = Get-ChildItem $dir -Recurse -Include "AssemblyInfo.cs" | where {$_ -match $projectNamespace}
	
    foreach($file in $info_files)
    {
        Setversion $file $version
    }
}

$projectNamespace=$args[0]
$solutionDir=$args[1]
$version = getVersion
setVersionInDir $projectNamespace $solutionDir $version
