@echo off
setlocal
set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" (
  echo .NET Framework 4 compiler was not found.
  exit /b 1
)
if not exist "%~dp0dist" mkdir "%~dp0dist"
"%CSC%" /nologo /target:winexe /platform:x64 /optimize+ "/win32manifest:%~dp0src\app.manifest" "/out:%~dp0dist\Apollo-Ship.exe" /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Core.dll /r:System.Web.Extensions.dll "%~dp0src\App.cs" "%~dp0src\NativeUi.cs" "%~dp0src\AsciiDisplay.cs"
if errorlevel 1 exit /b 1
if exist "%~dp0dist\Apollo Ship.exe" (
  del /q "%~dp0dist\Apollo Ship.exe"
  if exist "%~dp0dist\Apollo Ship.exe" (
    echo Close the old Apollo Ship app, then run build.cmd again.
    exit /b 1
  )
)
if exist "%~dp0dist\YT Music Download.exe" (
  del /q "%~dp0dist\YT Music Download.exe"
  if exist "%~dp0dist\YT Music Download.exe" (
    echo Close the old YT Music Download app, then run build.cmd again.
    exit /b 1
  )
)
if exist "%~dp0dist\index.html" del /q "%~dp0dist\index.html"
if exist "%~dp0dist\YT Music Download.exe.config" del /q "%~dp0dist\YT Music Download.exe.config"
if exist "%~dp0dist\Apollo Ship.exe.config" del /q "%~dp0dist\Apollo Ship.exe.config"
if exist "%~dp0dist\Apollo-Ship.exe.config" del /q "%~dp0dist\Apollo-Ship.exe.config"
if exist "%~dp0dist\Microsoft.Web.WebView2.Core.dll" del /q "%~dp0dist\Microsoft.Web.WebView2.Core.dll"
if exist "%~dp0dist\Microsoft.Web.WebView2.WinForms.dll" del /q "%~dp0dist\Microsoft.Web.WebView2.WinForms.dll"
if exist "%~dp0dist\WebView2Loader.dll" del /q "%~dp0dist\WebView2Loader.dll"
for %%I in ("%~dp0dist\Apollo-Ship.exe") do (
  echo Built %%~zI bytes: %%~fI
  if %%~zI GTR 1048576 (
    echo The app exceeded the 1 MB launcher target.
    exit /b 1
  )
)
