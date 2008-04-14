================================================================================
WwwPyFilter 0.2.1.1
Copyright ©2008 Liam Kirton <liam@int3.ws>

4th April 2008
http://int3.ws/
================================================================================

Overview:
---------

WwwPyFilter is a simple web application proxy, built upon the WwwProxy library,
that employs IronPython scripts in order to dynamically modify web traffic.

Please email me (liam@int3.ws) if you would like more information and/or
a deeper technical explanation of the application.

Features:
---------

WwwPyFilter features the ability to capture, modify and transmit both HTTP and
HTTPS traffic.

Usage:
------

Execute makecert.bat to generate a self-signed certificate to use for SSL
traffic interception. Copy WwwProxy.cer to the relevant working directory in
order to allow WwwPyFilter to locate it.

> WwwPyFilter.exe
> WwwPyFilter.exe 8081 (Specify custom port)
> WwwPyFilter.exe 8081 127.0.0.1:8080 (Specify custom port & remote proxy)

================================================================================