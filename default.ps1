Framework "4.0"

Properties {
    $base_dir = Split-Path $psake.build_script_file
    $build_artifacts_dir = "$base_dir\build"
    $solution_file = "$base_dir\solutionizer.sln"
}

task Default -depends Clean, Compile

include .\psake_ext.ps1

task CreateAssemblyInfo {
    $gittag = & git describe --tags --long
    $gittag

    if (!($gittag -match '^v(?<major>\d+)\.(?<minor>\d+)(\.(?<patch>\d+))?-(?<revision>\d+)-(?<commit>[a-z0-9]+)$')){
        throw "$gittag is not recognized"
    }
    $majorVersion = $matches['major']
    $minorVersion = $matches['minor']
    $patchVersion = $matches['patch']
    $revisionCount = $matches['revision']
    $commitVersion = $matches['commit']

    Write-Host "Current version: $majorVersion.$minorVersion.$patchVersion.$revisionCount-$commitVersion"

    $version = "$majorVersion.$minorVersion.$patchVersion.$revisionCount"
    $fileversion = "$majorVersion.$minorVersion.$patchVersion.$revisionCount"
    $asmInfo = "using System.Reflection;

[assembly: AssemblyVersion(""$majorVersion.$minorVersion.0"")]
[assembly: AssemblyInformationalVersion(""$majorVersion.$minorVersion.$patchVersion.$revisionCount-$commitVersion"")]
[assembly: AssemblyFileVersion(""$majorVersion.$minorVersion.$patchVersion.$revisionCount"")]"

    $file = Join-Path $base_dir "CommonAssemblyInfo.cs"
    Write-Host "Generating assembly info file $file"
    Write-Output $asmInfo > $file
}

task Compile -depends CreateAssemblyInfo {
    Write-Host "Building $solution_file" -ForegroundColor Green
    Exec { msbuild "$solution_file" /v:minimal /p:OutDir=$build_artifacts_dir }
}

Task Clean {
    Write-Host "Creating BuildArtifacts directory" -ForegroundColor Green
    if (Test-Path $build_artifacts_dir) {   
        rd $build_artifacts_dir -rec -force | out-null
    }
    
    mkdir $build_artifacts_dir | out-null
    
    Write-Host "Cleaning $solution_file" -ForegroundColor Green
    Exec { msbuild $solution_file /t:Clean /p:Configuration=Release /v:minimal } 
}

Task Package {
    Write-Host "Creating package" -ForegroundColor Green   

    $wixUrl = "http://wixtoolset.org/releases/v3.6.2823.0/wix36-binaries.zip"

    $wixZipFile = "$base_dir\packages\wix.zip"
    $wix_dir = "$base_dir\packages\wix-3.6"
    $candle_path = "$wix_dir\candle.exe"
    $light_path = "$wix_dir\light.exe"
    $heat_path = "$wix_dir\heat.exe"

    if (!(Test-Path $candle_path)) {
        if (!(Test-Path $wixZipFile)) {
            Write-Host "Downloading WiX toolset from $wixUrl"
            Get-WebFile -url $wixUrl -fileName $wixZipFile
        }

	    if (!(Test-Path $wix_dir)) {
        	New-Item $wix_dir -Type directory | Out-Null
		}
        Write-Host "Extracting WiX toolset to $wix_dir"
        Expand-ZipFile -zipPath $wixZipFile -destination $wix_dir
    }

	#& "$heat_path" dir "$build_artifacts_dir" -cg SolutionizerFiles -gg -scom -sreg -sfrag -srd -dr INSTALLLOCATION -var env.SolutionizerFiles -out "$build_artifacts_dir\FilesFragment.wxs"
    #& "$heat_path" dir "$build_artifacts_dir" -cg SolutionizerFiles -gg -scom -sreg -sfrag -srd -dr INSTALLLOCATION -out "$build_artifacts_dir\FilesFragment.wxs"

    #Exec { &"$candle_path" .\Solutionizer.wxs -dARTIFACTSDIR=$build_artifacts_dir -out ""$build_artifacts_dir\\"" }
    Exec { &"$candle_path" .\Solutionizer.wxs "-dARTIFACTSDIR=$build_artifacts_dir" -out `"$build_artifacts_dir\\`" }

    #Exec { &"$light_path" ""$build_artifacts_dir\Solutionizer.wixobj"" }
    Exec { &"$light_path" "$build_artifacts_dir\Solutionizer.wixobj" "-dARTIFACTSDIR=$build_artifacts_dir" -out `"$build_artifacts_dir\\solutionizer.msi`" }
}