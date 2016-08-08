SET dir="c:\Program Files (x86)\Steam\steamapps\common\RimWorld\Mods\BuildProductive-0.15\"

rd /s /q %dir%

xcopy *.* %dir% /s /exclude:exclude.txt