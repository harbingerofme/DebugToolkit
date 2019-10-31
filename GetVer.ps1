param([string]$ProjectName)
$scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$Bepin = $scriptDir + "\libs\BepInEx.dll"
$AssemblyName = $scriptDir + "\libs\" + $ProjectName + ".dll"
Add-Type -Path $Bepin
Add-Type -Path $AssemblyName
$modver = ((([appdomain]::currentdomain.GetAssemblies() | Where-Object location -match $ProjectName).gettypes() | Where-Object name -eq $ProjectName).getmembers() | Where-Object name -eq 'GetModVer')
$modver.Invoke($null, $null)