cd ..
sc create XiHan.Framework binPath= %~dp0XiHan.Framework.exe start= auto 
sc description XiHan.Framework "XiHan.Framework"
Net Start XiHan.Framework
pause
