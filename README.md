Interactive C# REPL addin for MonoDevelop/Xamarin Studio

# Hacking
Before you start, open the CSharpReplAddin project file and find the MonoDevelopBasePath property. Set this to the directory where the version of MonoDevelop or Xamarin Studio that you want to build against lives.

Also set the MonoDevelopExecutableName property to the filename (with extension) of your MonoDevelop/Xamarin Studio executable.

This project uses NuGet, so you'll probably want to install the NuGet addin.

<!--

Debugging on Windows in Visual Studio

* Use the DebugWindows/AnyCpu configuration
* In the CSharpReplAddin project's Debug configuration, set the startup program to ..\monodevelop\main\build\bin\MonoDevelop.exe, and the working directory to C:\Users\rock361\Development\thirdpartywork\monodevelop\main\build\bin\
* In the MonoDevelop main/build/bin directory, edit the MonoDevelop.exe.addins file to include <Directory include-subdirs="false">..\..\..\..\CSharpReplAddin\CSharpReplAddin\bin\Debug</Directory> inside the <Addins> section

-->
