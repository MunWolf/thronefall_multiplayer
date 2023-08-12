try {
    $sln_path = $Args[0]
    $input_path = $Args[1]

    $config = Get-Content (Join-Path $sln_path install.cfg) | ConvertFrom-StringData

    function CopyDll {
        param (
            $dll
        )

        Write-Output "Installing $(Join-Path $config.InstallPath "$($dll).dll")"
        $null = New-Item -ItemType File -Path (Join-Path $config.InstallPath "$($dll).dll") -Force
        $null = Copy-Item (Join-Path $input_path "$($dll).dll") (Join-Path $config.InstallPath "$($dll).dll") -Force
        if (Test-Path -Path (Join-Path $input_path "$($dll).pdb") -PathType Leaf) {
            $null = Copy-Item (Join-Path $input_path "$($dll).pdb") (Join-Path $config.InstallPath "$($dll).pdb") -Force
        }
    }

    $dlls = (
        'com.badwolf.thronefall_mp',
        'MMHOOK_Assembly-CSharp',
        'UniverseLib.Mono'
    )

    $dlls | ForEach-Object {
        CopyDll -dll $_
    }
} catch { throw 1; }