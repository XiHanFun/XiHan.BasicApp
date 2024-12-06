cd ..
sc create XiHan.App binPath= %~dp0XiHan.App.exe start= auto 
sc description XiHan.App "XiHan.App"
Net Start XiHan.App
pause
