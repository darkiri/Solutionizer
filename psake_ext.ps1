function Expand-ZipFile {
    param(
       $zipPath,
       $destination,
       [switch] $quiet
    )

    $shellApplication = new-object -com shell.application
    $zipPackage = $shellApplication.NameSpace($zipPath)
    $destinationFolder = $shellApplication.NameSpace($destination) 

    # CopyHere vOptions Flag # 4 - Do not display a progress dialog box. 
    # 16 - Respond with "Yes to All" for any dialog box that is displayed. 
    #$destinationFolder.CopyHere($zipPackage.Items(),20) 
    $total = $zipPackage.Items().Count
    $progress = 1;
    foreach ($zipFile in $zipPackage.Items()) {
        if(!$quiet) {
            Write-Progress "Extracting $zipPath" "Extracting $($zipFile.Name)" -id 0 -percentComplete (($progress/$total)*100)        
        }

        $destinationFolder.CopyHere($zipFile,20) 
        $progress++
    }
    if(!$quiet) {
        Write-Progress "Extracted $zipPath" "Extracted $total items" -id 0
    }
}

# Get-WebFile 3.6 (aka wget for PowerShell)
# by Joel Bennett http://poshcode.org/417
function Get-WebFile {
   param( 
      $url = (Read-Host "The URL to download"),
      $fileName = $null,
      [switch]$Passthru,
      [switch]$quiet
   )
   
   $req = [System.Net.HttpWebRequest]::Create($url);
   $res = $req.GetResponse();
 
   if($fileName -and !(Split-Path $fileName)) {
      $fileName = Join-Path (Get-Location -PSProvider "FileSystem") $fileName
   } 
   elseif((!$Passthru -and ($fileName -eq $null)) -or (($fileName -ne $null) -and (Test-Path -PathType "Container" $fileName)))
   {
      [string]$fileName = ([regex]'(?i)filename=(.*)$').Match( $res.Headers["Content-Disposition"] ).Groups[1].Value
      $fileName = $fileName.trim("\/""'")
      if(!$fileName) {
         $fileName = $res.ResponseUri.Segments[-1]
         $fileName = $fileName.trim("\/")
         if(!$fileName) { 
            $fileName = Read-Host "Please provide a file name"
         }
         $fileName = $fileName.trim("\/")
         if(!([IO.FileInfo]$fileName).Extension) {
            $fileName = $fileName + "." + $res.ContentType.Split(";")[0].Split("/")[1]
         }
      }
      $fileName = Join-Path (Get-Location -PSProvider "FileSystem") $fileName
   }
   if($Passthru) {
      $encoding = [System.Text.Encoding]::GetEncoding( $res.CharacterSet )
      [string]$output = ""
   }
 
   if($res.StatusCode -eq 200) {
      [int]$goal = $res.ContentLength
      $reader = $res.GetResponseStream()
      if($fileName) {
         $writer = new-object System.IO.FileStream $fileName, "Create"
      }
      [byte[]]$buffer = new-object byte[] 4096
      [int]$total = [int]$count = 0
      do
      {
         $count = $reader.Read($buffer, 0, $buffer.Length);
         if($fileName) {
            $writer.Write($buffer, 0, $count);
         } 
         if($Passthru){
            $output += $encoding.GetString($buffer,0,$count)
         } elseif(!$quiet) {
            $total += $count
            if($goal -gt 0) {
               Write-Progress "Downloading $url" "Saving $total of $goal" -id 0 -percentComplete (($total/$goal)*100)
            } else {
               Write-Progress "Downloading $url" "Saving $total bytes..." -id 0
            }
         }
      } while ($count -gt 0)
      
      $reader.Close()
      if($fileName) {
         $writer.Flush()
         $writer.Close()
      }
      if($Passthru){
         $output
      }
   }
   $res.Close(); 
   if($fileName) {
      ls $fileName
   }
}