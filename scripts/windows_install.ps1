# SPDX-FileCopyrightText: None
# SPDX-License-Identifier: CC0-1.0

Write-Output "[INFO] Installing tldr-sharp"

if ($PSVersionTable.PSVersion.Major -lt 5) {
    throw "Powershell v5 or newer is required."
}

[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::TLS12

$apiUrl = "https://api.github.com/repos/principis/tldr-sharp/releases/latest"

$webClient = new-object system.net.webclient
$webClient.Headers.Add("user-agent", "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; .NET CLR 1.0.3705;)")

$json = $webClient.DownloadString($apiUrl) | ConvertFrom-Json


$downloadUrl = ""

Foreach ($item in $json.assets) {
    if ($item.browser_download_url.IndexOf("windows") -gt 0) {
        
        if ([Environment]::Is64BitProcess) {
            if ($item.browser_download_url.IndexOf("x64") -gt 0) {
                $downloadUrl = $item.browser_download_url
                break;
            }
        } else {
            $downloadUrl = $item.browser_download_url
            break;
        }
    } else {
        continue
    }
}

if ([string]::IsNullOrEmpty($downloadUrl)){
    throw "Download url not found."
}

Write-Output "Downloading tldr-sharp from : $downloadUrl"

if ($null -eq $env:TEMP) {
  $env:TEMP = Join-Path $env:SystemDrive 'temp'
}
$tempDir = Join-Path $env:TEMP "tldr-sharp"

if (![System.IO.Directory]::Exists($tempDir)) {[void][System.IO.Directory]::CreateDirectory($tempDir)}

$file = Join-Path $tempDir "tldr-sharp.zip"

$webClient.DownloadFile($downloadUrl, $file)

Expand-Archive -Path "$file" -DestinationPath $tempDir -Force
Remove-Item -Path $file

$tldrPath = "$env:ALLUSERSPROFILE\tldr-sharp"

if ([System.IO.Directory]::Exists($tldrPath)) {
    Remove-Item -Path $tldrPath -Recurse -Force
}
[void][System.IO.Directory]::CreateDirectory($tldrPath)

Copy-Item -Path "$tempDir\*" -Destination $tldrPath
Remove-Item -Path $tempDir -Recurse -Force

if (([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {
    New-Item -ItemType SymbolicLink -Target "$tldrPath\tldr-sharp.exe" -Path "$tldrPath/tldr.exe"
    New-Item -ItemType SymbolicLink -Target "$tldrPath\tldr-sharp.exe.config" -Path "$tldrPath/tldr.exe.config"
} else {
    Copy-Item -Path "$tldrPath\tldr-sharp.exe" -Destination "$tldrPath\tldr.exe"
    Copy-Item -Path "$tldrPath\tldr-sharp.exe.config" -Destination "$tldrPath\tldr.exe.config"
}

if ($($env:Path).ToLower().Contains($($tldrPath).ToLower()) -eq $false) {

    if (([Security.Principal.WindowsPrincipal] `
    [Security.Principal.WindowsIdentity]::GetCurrent() `
    ).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)) {

    [Environment]::SetEnvironmentVariable(
        "Path",
        [Environment]::GetEnvironmentVariable("Path", [EnvironmentVariableTarget]::Machine) + ";$tldrPath",
        [EnvironmentVariableTarget]::Machine)
    } else {
        Write-Warning "Setting tldr-sharp Environment Variable on USER and not SYSTEM variables. 
        This is due to either non-administrator install OR the process you are running is not being run as an Administrator."
        [Environment]::SetEnvironmentVariable(
        "Path",
        [Environment]::GetEnvironmentVariable("Path", [EnvironmentVariableTarget]::User) + ";$tldrPath",
        [EnvironmentVariableTarget]::User)
    }
}