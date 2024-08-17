Stop-Process -Name TravellersRest -ErrorAction SilentlyContinue


dotnet build
cp -r .\output\Debug\netstandard2.1\rbk-*.dll .

$mods = Get-ChildItem -Path . -Filter rbk-*.dll;
$mods | ForEach-Object { $mod = [System.IO.Path]::GetFileNameWithoutExtension($_.FullName); 7z a -tzip "$mod.zip" "$mod.dll" 1> $null }

cp -force .\rbk-tr-*.dll "C:\Program Files (x86)\Steam\steamapps\common\Travellers Rest\Windows\BepInEx\plugins\."

."C:\Program Files (x86)\Steam\steamapps\common\Travellers Rest\Windows\TravellersRest.exe"