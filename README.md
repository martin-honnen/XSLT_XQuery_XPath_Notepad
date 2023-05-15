# XSLT_XQuery_XPath_Notepad
XSLT 3.0, XQuery 3.1, XPath3.1 Notepad using Saxon-HE 10.9 .NET.

This a first stab to provide XSLT 3.0 and XQuery 3.1 and XPath 3.1 fiddling inside of a Windows 10/11 .NET framework desktop app instead of inside the browser.

The big advantage is that you can load input files (e.g. XML and JSON) and XSLT or XQuery or XPath code files from the local file system (including of course, a cloud drive like Microsoft OneDrive).

The downloadable zip contains a setup.exe which should install the app on your system, depending on the availability of the existing .NET framework version(s) on your system the installer might request for permission to install the current version 4.8 of the .NET framework.

The app also uses the so called WebView2 WPF browser control, as far as I understand its runtime should by now be part of any supported Windows 10 and 11 release. If you nevertheless see errors in the status bar about webview2 then consider installing the runtime from https://developer.microsoft.com/en-us/microsoft-edge/webview2/#download-section.



