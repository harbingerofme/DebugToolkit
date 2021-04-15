git clone https://github.com/harbingerofme/RoR2Libs.git
rmdir /S /Q RoR2Libs\scripts\
del RoR2Libs\Managed\Assembly-CSharp.dll
cp -r ./RoR2Libs ./
rmdir /S /Q RoR2Libs
del README.MD
del .gitignore

