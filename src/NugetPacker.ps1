try
{
	$path = "C:\Users\Uffe\Documents\GitHub\XSockets.Clients\src"
	$outputdir = "nuget\builds"
	$version = "6.2.0"
	$deploy = $true;

	$file= "xsockets.client.nuspec"
	$name = "xsockets.client"
	if(!(Test-Path "$($path)\$($outputdir)\$($name).$($version)")){
		New-Item -Path "$($path)\$($outputdir)\$($name).$($version)" -ItemType Directory
	}

	Invoke-Expression "$($path)\nuget.exe pack $($path)\$($file) -Version $($version) -OutputDirectory $($path)\$($outputdir)\$($name).$($version)"
	if($deploy){
		Invoke-Expression "$($path)\nuget.exe push $($path)\$($outputdir)\$($name).$($version)\$($name).$($version).nupkg"
	}
	pause
}
catch
{
    Write-Error $_.Exception.ToString()
    Read-Host -Prompt "The above error occurred. Press Enter to exit."
}