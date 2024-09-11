Stop-Process -Name TravellersRest -ErrorAction SilentlyContinue

dotnet build
cp .\obj\Debug\netstandard2.1\rbk-tr.dll .
$mods = Get-ChildItem -Path . -Filter rbk-tr.dll;
cp -force .\rbk-tr.dll "C:\Program Files (x86)\Steam\steamapps\common\Travellers Rest\Windows\BepInEx\plugins\."
."C:\Program Files (x86)\Steam\steamapps\common\Travellers Rest\Windows\TravellersRest.exe"