try {
    Write-Host "- Installing"
    $sln_path = $Args[0]
    $input_path = $Args[1]

    $config = Get-Content (Join-Path $sln_path install.cfg) | ConvertFrom-StringData

    function CopyDll {
        param (
            $dll
        )

        Write-Host "Installing $(Join-Path $config.InstallPath "$($dll).dll")"
        $null = Copy-Item (Join-Path $input_path "$($dll).dll") (Join-Path $config.InstallPath "$($dll).dll") -Force
        if (Test-Path -Path (Join-Path $input_path "$($dll).pdb") -PathType Leaf) {
            $null = Copy-Item (Join-Path $input_path "$($dll).pdb") (Join-Path $config.InstallPath "$($dll).pdb") -Force
        }
    }

    $null = New-Item -ItemType Directory -Path $config.InstallPath -Force
    $dlls = (
        'com.badwolf.thronefall_mp'
    )

    $dlls | ForEach-Object {
        CopyDll -dll $_
    }
} catch {
    Write-Host $_
    throw 1;
}
