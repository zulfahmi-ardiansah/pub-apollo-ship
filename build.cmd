@echo off
setlocal
set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" (
  echo .NET Framework 4 compiler was not found.
  exit /b 1
)
if not exist "%~dp0dist" mkdir "%~dp0dist"
"%CSC%" /nologo /target:winexe /platform:x64 /optimize+ "/win32manifest:%~dp0src\app.manifest" "/out:%~dp0dist\YT Music Download.exe" /r:System.Windows.Forms.dll /r:System.Drawing.dll /r:System.Core.dll /r:System.Web.Extensions.dll "/r:%~dp0vendor\Microsoft.Web.WebView2.Core.dll" "/r:%~dp0vendor\Microsoft.Web.WebView2.WinForms.dll" "/resource:%~dp0vendor\Microsoft.Web.WebView2.Core.dll,Apollo.Core.dll" "/resource:%~dp0vendor\Microsoft.Web.WebView2.WinForms.dll,Apollo.WinForms.dll" "/resource:%~dp0vendor\WebView2Loader.dll,Apollo.Loader.dll" "/resource:%~dp0src\index.html,Apollo.index.html" "%~dp0src\App.cs"
if errorlevel 1 exit /b 1
if exist "%~dp0dist\index.html" del /q "%~dp0dist\index.html"
if exist "%~dp0dist\YT Music Download.exe.config" del /q "%~dp0dist\YT Music Download.exe.config"
if exist "%~dp0dist\Microsoft.Web.WebView2.Core.dll" del /q "%~dp0dist\Microsoft.Web.WebView2.Core.dll"
if exist "%~dp0dist\Microsoft.Web.WebView2.WinForms.dll" del /q "%~dp0dist\Microsoft.Web.WebView2.WinForms.dll"
if exist "%~dp0dist\WebView2Loader.dll" del /q "%~dp0dist\WebView2Loader.dll"
for %%I in ("%~dp0dist\YT Music Download.exe") do (
  echo Built %%~zI bytes: %%~fI
  if %%~zI GTR 1048576 (
    echo The app exceeded the 1 MB launcher target.
    exit /b 1
  )
)
