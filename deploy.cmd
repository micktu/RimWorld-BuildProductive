SET dir="c:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\BuildProductive-0.1\"
SET srcDir="c:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\BuildProductive-0.1\Source"

rd /s /q %dir%

xcopy *.* %dir% /s /exclude:exclude.txt

