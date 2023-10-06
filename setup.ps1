$path = Read-Host -Prompt 'Input the path to your Thronefall installation (default: C:\Program Files (x86)\Steam\steamapps\common\Thronefall)'
$mod_name = "ThronefallMP"

if ($path -eq "") {
    $path = "C:\Program Files (x86)\Steam\steamapps\common\Thronefall"
}

$dlls = (
    "Assembly-CSharp",
    "AstarPathfindingProject",
    "MoreMountains.Feedbacks",
    "MPUIKit",
    "Rewired_Core",
    "ShapesRuntime",
    "Unity.TextMeshPro",
    "UnityEngine.UI",
    "UnityEngine.CoreModule",
    "UnityEngine",
    "com.rlabrecque.steamworks.net"
)

Write-Host "Setting up lib directory"
if (Test-Path (Join-Path $path Thronefall.exe) -PathType Leaf) {
    $library_path = Join-Path $path Thronefall_Data\Managed
    if (!(Test-Path -Path .\lib)) {
        New-Item -ItemType Directory -Path .\ -Name lib
    }

    $dlls | ForEach-Object {
        Copy-Item (Join-Path $library_path "$_.dll") ".\lib\$_.dll"
    }

    $cfg = "InstallPath = $([RegEx]::Escape($(Join-Path $path BepInEx\plugins\$mod_name)))"
    Out-File -InputObject $cfg -FilePath .\install.cfg
}
else {
    Write-Host "Thronefall.exe not found, terminating."
}

Write-Host "Adding references"
$project = Get-Content Main/$mod_name.csproj
