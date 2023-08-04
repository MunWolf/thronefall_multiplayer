$path = Read-Host -Prompt 'Input the path to your Thronefall installation'


if (Test-Path (Join-Path $path Thronefall.exe) -PathType Leaf) {
    $library_path = Join-Path $path Thronefall_Data\Managed
    Copy-Item (Join-Path $library_path Assembly-CSharp.dll) .\lib\Assembly-CSharp.dll
    Copy-Item (Join-Path $library_path AstarPathfindingProject.dll) .\lib\AstarPathfindingProject.dll
    Copy-Item (Join-Path $library_path MoreMountains.Feedbacks.dll) .\lib\MoreMountains.Feedbacks.dll
    Copy-Item (Join-Path $library_path MPUIKit.dll) .\lib\MPUIKit.dll
    Copy-Item (Join-Path $library_path Rewired_Core.dll) .\lib\Rewired_Core.dll
    Copy-Item (Join-Path $library_path Unity.TextMeshPro.dll) .\lib\Unity.TextMeshPro.dll
    Copy-Item (Join-Path $library_path UnityEngine.UI.dll) .\lib\UnityEngine.UI.dll
    $cfg = "InstallPath = $([RegEx]::Escape($(Join-Path $path BepInEx\plugins\ThronefallMultiplayer)))"
    Out-File -InputObject $cfg -FilePath .\install.cfg
}
else {
    Write-Host "Thronefall.exe not found, terminating."
}