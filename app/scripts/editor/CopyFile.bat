@echo off

REM 将 CMD 窗口的代码页修改为 UTF-8编码
chcp 65001

REM 设置原文件和目标路径变量
set SRC_FOLDER=%cd%
set CLASS_DEST_PATH="C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\ItemTemplates\CSharp\Code\2052\Class"
set CLASS_CORE_DEST_PATH="C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\ItemTemplates\AspNetCore\Code\1033\Class"
set INTERFACE_DEST_PATH="C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\ItemTemplates\CSharp\Code\2052\Interface"
set INTERFACE_CORE_DEST_PATH="C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\ItemTemplates\AspNetCore\Code\1033\Interface"
set CONTROLLER_DEST_PATH="C:\Program Files\Microsoft Visual Studio\2022\Community\Common7\IDE\ItemTemplates\AspNetCore\Web\ASP.NET\1033\WebApiEmptyController"

echo 当前路径："%SRC_FOLDER%"

REM 复制 Class.cs 文件到目标文件夹
if exist "%SRC_FOLDER%\Class.cs" (
  copy /Y "%SRC_FOLDER%\Class.cs" %CLASS_DEST_PATH%
  copy /Y "%SRC_FOLDER%\Class.cs" %CLASS_CORE_DEST_PATH%
  echo 复制 Class.cs 文件成功！
) else (
  echo Class.cs 文件不存在，请检查后重试！
)

REM 复制 Interface.cs 文件到目标文件夹
if exist "%SRC_FOLDER%\Interface.cs" (
  copy /Y "%SRC_FOLDER%\Interface.cs" %INTERFACE_DEST_PATH%
  copy /Y "%SRC_FOLDER%\Interface.cs" %INTERFACE_CORE_DEST_PATH%
  echo 复制 Interface.cs 文件成功！
) else (
  echo Interface.cs 文件不存在，请检查后重试！
)

REM 复制 Controller.cs 文件到目标文件夹
if exist "%SRC_FOLDER%\Controller.cs" (
  copy /Y "%SRC_FOLDER%\Controller.cs" %CONTROLLER_DEST_PATH%
  echo 复制 Controller.cs 文件成功！
) else (
  echo Controller.cs 文件不存在，请检查后重试！
)

pause
