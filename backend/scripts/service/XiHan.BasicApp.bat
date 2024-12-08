cd ..
sc create XiHan.BasicApp binPath= %~dp0XiHan.BasicApp.exe start= auto 
sc description XiHan.BasicApp "XiHan.BasicApp"
Net Start XiHan.BasicApp
pause
