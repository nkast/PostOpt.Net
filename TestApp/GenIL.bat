..\PostOpt\bin\Release\PostOpt.exe bin\Release\testApp.exe
..\PostOpt\bin\Debug\PostOpt.exe bin\Debug\testApp.exe


"C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.6 Tools\ILDASM.exe" bin\Release\testApp.exe /PUBONLY /SOURCE /OUT:bin\Release\testApp.il
"C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.6 Tools\ILDASM.exe" bin\Release\testApp.optmzd.exe /PUBONLY /SOURCE /OUT:bin\Release\testApp.optmzd.il

"C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.6 Tools\ILDASM.exe" bin\Debug\testApp.exe /PUBONLY /SOURCE /OUT:bin\Debug\testApp.il
"C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.6 Tools\ILDASM.exe" bin\Debug\testApp.optmzd.exe /PUBONLY /SOURCE /OUT:bin\Debug\testApp.optmzd.il

@pause