git clone --single-branch --branch bep150 https://github.com/Paddywaan/RoR2Libs
del RoR2Libs\.gitattributes
del RoR2Libs\.gitignore
del RoR2Libs\RoR2Libs.sln
rmdir /S /Q RoR2Libs\.git
copy RoR2Libs\libs\*.dll .\
rmdir /S /Q RoR2Libs