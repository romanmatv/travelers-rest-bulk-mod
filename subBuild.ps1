Stop-Process -Name TravellersRest -ErrorAction SilentlyContinue

dotnet build
cp .\obj\Debug\netstandard2.1\rbk-tr.dll .
$mods = Get-ChildItem -Path . -Filter rbk-tr.dll;
cp -force .\rbk-tr.dll "D:\SteamLibrary\steamapps\common\Travellers Rest\Windows\BepInEx\plugins\."
."D:\SteamLibrary\steamapps\common\Travellers Rest\Windows\TravellersRest.exe"