SET dir="c:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\BuildProductive-0.30\"

rd /s /q %dir%

xcopy *.* %dir% /s /exclude:exclude.txt