param([string]$DirectoryPath)

Get-ChildItem -Path $DirectoryPath -Recurse | Where-Object { $_.Length -gt 100MB } | 
Select-Object Name, Length, LastWriteTime | ConvertTo-Json