Write-Host "Fixing protobuf...";

Get-ChildItem "../../obj/" -Recurse -Filter *.cs | 
Foreach-Object {
	$fileName = $_.FullName
	if($fileName -Match "Protobufs") {
		Write-Host ("Parsing file: " + $_.FullName)

		((Get-Content -path $_.FullName -Raw) -replace '{ get; }','{ get; set; }') | Set-Content -Path $_.FullName
	}
}