$SolutionFile = $args[0]
$ReportPath = $args[1]

if (-not $SolutionFile) {
    Write-Error "'$SolutionFile' does not exist."
    exit 1
}

Write-Host "LibraryDependencyReport for solution" $SolutionFile "will be generated to" $ReportPath "."

$processOptions = @{
    FilePath     = "dotnet"
    ArgumentList = @("nuget-license", "-i", "`"$solutionFile`"", "-o", "jsonPretty", "-fo", "`"$reportPath`"")
    Wait         = $true
    NoNewWindow  = $true
    PassThru     = $true
}

$process = Start-Process @processOptions

# NOTE: nuget-license itself returns a non-zero exit code (e.g. 4) whenever it could not
# resolve a license for at least one package. That is expected and handled below via our
# own strict validation *after* applying manual overrides - so we do NOT treat this as fatal here.
if ($process.ExitCode -ne 0) {
    Write-Host "nuget-license finished with exit code $($process.ExitCode) (packages with missing license information found - will be checked against overrides below)."
}

if (-not (Test-Path $ReportPath)) {
    Write-Error "Expected license report was not created at '$ReportPath'."
    exit 1
}

$packages = Get-Content $ReportPath -Raw | ConvertFrom-Json

function Test-MissingLicense {
    param($package)
    return [string]::IsNullOrWhiteSpace($package.License) -or ($package.ValidationErrors -and $package.ValidationErrors.Count -gt 0)
}

# Merge manual license overrides for packages where nuget-license could not
# determine license information from the package's nuspec metadata.
$overridesPath = Join-Path $PSScriptRoot "LicenseOverrides.json"
$overrides = @()

if (Test-Path $overridesPath) {
    $overrides = Get-Content $overridesPath -Raw | ConvertFrom-Json
}

foreach ($package in $packages) {
    if (Test-MissingLicense $package) {
        $override = $overrides | Where-Object { $_.packageId -eq $package.PackageId } | Select-Object -First 1

       if ($override) {
            $package | Add-Member -NotePropertyName "License" -NotePropertyValue $override.license -Force
            $package | Add-Member -NotePropertyName "LicenseUrl" -NotePropertyValue $override.licenseUrl -Force
            $package.ValidationErrors = @()
            Write-Host "Applied license override for" $package.PackageId "->" $override.license
        }
    }
}

$packages | ConvertTo-Json -Depth 10 | Set-Content -Path $ReportPath -Encoding utf8

# Strict validation: after applying overrides, no package may remain without a resolved license.
$stillMissing = $packages | Where-Object { Test-MissingLicense $_ }

if ($stillMissing.Count -gt 0) {
    Write-Error "The following $($stillMissing.Count) package(s) have no resolved license information. Add an entry for them in '$overridesPath':"

    foreach ($missing in $stillMissing) {
        Write-Error " - $($missing.PackageId) $($missing.PackageVersion)"
    }

    exit 1
}

Write-Host "License check passed: all $($packages.Count) package(s) have a resolved license."
exit 0
