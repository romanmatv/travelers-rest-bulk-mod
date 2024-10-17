Stop-Process -Name TravellersRest -ErrorAction SilentlyContinue

rm -force -r "NexusMods/." 2> $null;

dotnet build
cp -r .\output\Debug\netstandard2.1\rbk-*.dll .

$targetReadme="read.me"

$mods = Get-ChildItem -Path . -Filter rbk-tr-*.dll;
$mods | ForEach-Object {
    $mod = [System.IO.Path]::GetFileNameWithoutExtension($_.FullName);
    $modFolder = $mod.replace('rbk-tr-', '');
    $nexusFolder = "NexusMods/$modFolder"
    7z a -tzip "$nexusFolder/$mod.zip" "$mod.dll" 1> $null;
    mv -Force "$mod.dll" "$modFolder/."
    cat "$modFolder/readme.md" > "$nexusFolder/$targetReadme" 2> $null;
    echo "" >> "$nexusFolder/$targetReadme";
    echo "## ChangeLog " >> "$nexusFolder/$targetReadme";
    echo "" >> "$nexusFolder/$targetReadme";
    cat "$modFolder/change-log.md" >> "$nexusFolder/$targetReadme" 2> $null;
    echo "" >> "$nexusFolder/$targetReadme";
    echo "" >> "$nexusFolder/$targetReadme";
    cat modUsageNotes.md >> "$nexusFolder/$targetReadme";
    python -c "from md2bbcode.main import process_readme;f=open('$nexusFolder/$targetReadme', encoding='utf-16');txt=f.read().replace('###### ', '').replace('#####', '');bbcode_output = process_readme(txt).replace('HEADING=1', 'HEADING=6').replace('HEADING=2', 'HEADING=5').replace('HEADING=3', 'HEADING=4').replace('[HEADING=','[size=').replace('[/HEADING]', '[/size]').replace('icode]', 'code]');f=open('$nexusFolder/read.bb', 'w');f.write(bbcode_output); f.close();"
}

Set-Variable -Name run -Value $args[0];

cp -force .\**\rbk-tr-*.dll "C:\Program Files (x86)\Steam\steamapps\common\Travellers Rest\Windows\BepInEx\plugins\.";


if ($run) {
    ."C:\Program Files (x86)\Steam\steamapps\common\Travellers Rest\Windows\TravellersRest.exe";
}