' Runs tools/steam_dev_guard.sh with no window, for the Windows task "DeadlockOfRain dev guard" (every 5 minutes;
' doc 03 section 5): the Melty app puts its release back on the Steam copy whenever it starts, without any Claude session.
Set fso = CreateObject("Scripting.FileSystemObject")
guard = fso.GetParentFolderName(WScript.ScriptFullName) & "\steam_dev_guard.sh"
CreateObject("WScript.Shell").Run """C:\Program Files\Git\bin\bash.exe"" """ & guard & """", 0, False
