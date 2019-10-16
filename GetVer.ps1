param([string]$ProjectName)
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$Bepin = $scriptDir + "\bin\Release\netstandard2.0\BepInEx.dll"
$AssemblyName = $scriptDir + "\bin\Release\netstandard2.0\" + $ProjectName + ".dll"
Add-Type -Path $Bepin
Add-Type -Path $AssemblyName
$modver = ((([appdomain]::currentdomain.GetAssemblies() | Where-Object location -match $ProjectName).gettypes() | Where-Object name -eq $ProjectName).getmembers() | Where-Object name -eq 'GetModVer')
$modver.Invoke($null, $null)