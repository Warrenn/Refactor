function Load-Variables
{
    $project = Get-Project
    $projectPath = Split-Path $project.FullName
    $contentPath = Join-Path $projectPath "Content"
    if(-not (Test-Path "$($contentPath)"))
    {
        Write-Error "$($project.FullName) is the incorrect project type the project needs to be a web area with a content folder $($contentPath)"
        Exit(1)
    }
    $scriptPath = Split-Path -Path $script:MyInvocation.MyCommand.Path -Parent
    $solution = $project.DTE.Solution.FullName
    $refactorPath = Join-Path $scriptPath "Refactor.exe"
    $projectName = $project.Name
    
    return @{ 
        project = $project;
        projectPath = $projectPath;
        contentPath = $contentPath;
        scriptPath = $scriptPath;
        solution = $solution;
        refactorPath = $refactorPath;
        projectName = $projectName
    }
}

function Add-Controller
{
    param([string] $area, [string] $controller, [string] $service)
    $vars = Load-Variables
    ."$($vars.refactorPath)" --solution "$($vars.solution)" --project "$($vars.projectName)" --refactory "AddController" --controller "$($controller)" --area "$($area)" --service "$($service)"
}
Export-ModuleMember -Function Add-Controller

function Add-DataService
{
    param([string] $controller)
    $vars = Load-Variables
    ."$($vars.refactorPath)" --solution "$($vars.solution)" --project "$($vars.projectName)" --refactory "AddDataService" --controller "$($controller)"
}
Export-ModuleMember -Function Add-DataService

function Add-Directive
{
    param([string] $area, [string] $directive)
    $vars = Load-Variables
    ."$($vars.refactorPath)" --solution "$($vars.solution)" --project "$($vars.projectName)" --refactory "AddDirective" --area "$($area)" --directive "$($directive)"
}
Export-ModuleMember -Function Add-Directive

function Add-Module
{
    param([string] $module)
    $vars = Load-Variables
    ."$($vars.refactorPath)" --solution "$($vars.solution)" --project "$($vars.projectName)" --refactory "AddModule" --module "$($module)"
}
Export-ModuleMember -Function Add-Module