@echo off

set solutionDir=%~1
set buildDir=%~2Assets\LibraryDependencyReport.json

echo LibraryDependencyReport for solution %solutionDir% will be generated to %buildDir%
dotnet list %solutionDir% package --format json > %buildDir%

exit /b %ERRORLEVEL%