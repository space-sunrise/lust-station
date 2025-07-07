@echo off
dotnet build
xcopy /s /i /y Resources\Locale publish\Locale
py Scripts\replace_nt.py --path publish\Locale