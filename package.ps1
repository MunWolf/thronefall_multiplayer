try {
    $dlls = (
        'com.badwolf.thronefall_mp'
    )

    $sln_path = $Args[0]
    $input_path = $Args[1]
    $configuration = $Args[2]
    $mod_name = "ThronefallMultiplayer"
    $identifier = if ($configuration -eq "Release") { "_$($configuration)" } Else { "" }
    $version = (Get-Item (Join-Path $input_path "com.badwolf.thronefall_mp.dll")).VersionInfo.ProductVersion
    $package_name = "$($mod_name)_$($version)$($identifier)"
    
    Write-Host "- Packaging"
    Write-Host "Configuration $($configuration)"
    Write-Host "Version $($version)"
    Write-Host "SolutionDir $($sln_path)"
    Write-Host "Creating package '$(Join-Path $sln_path "Packages/$($package_name)")'"
    $null = New-Item -ItemType Directory -Path (Join-Path $sln_path "Packages") -Force
    $null = New-Item -ItemType Directory -Path (Join-Path $sln_path "Packages/temp") -Force
    $null = Copy-Item -Path (Join-Path $sln_path "PackageAssets/*") -Destination (Join-Path $sln_path "Packages/temp") -Force

    $null = New-Item -ItemType Directory -Path (Join-Path $sln_path "Packages/temp/$($mod_name)_Mod") -Force

    function CopyDll {
        param (
            $dll
        )
        
        Write-Host "Copying $(Join-Path $input_path "$($dll).dll")"
        $null = Copy-Item (Join-Path $input_path "$($dll).dll") (Join-Path $sln_path "Packages/temp/$($mod_name)_Mod/$($dll).dll") -Force
    }

    $dlls | ForEach-Object {
        CopyDll -dll $_
    }

    $manifest = (Join-Path $sln_path "Packages/temp/manifest.json")
    (Get-Content -Path $manifest).Replace('{VERSION}', $version) | Set-Content -Path $manifest


    $null = Compress-Archive `
        -CompressionLevel Optimal `
        -Path (Join-Path $sln_path "Packages/temp/*") `
        -DestinationPath (Join-Path $sln_path "Packages/$($package_name).zip") `
        -Force
    
    $null = Remove-Item -Path (Join-Path $sln_path "Packages/temp") -Force -Recurse
} catch {
    Write-Host $_
    throw 1;
}