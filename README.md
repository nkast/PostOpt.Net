# PostOpt.Net
A post build tool to weave your assemblies for better performance.
Maintain clean readable code with full performance.

The following code where r,a,b are Vector2:
 r = a + b;
is weaved post-build to:
 Vector2.Add(ref a, ref b, out r);


*This tool is currently in a proof of concept / expirimental stage.*

PostOpt.exe will search all methods in the assembly for calls to Vector2.op_Addition() 
and replace it with a similar call with ref arguments.

for example:
If the intstruction before op_Addition is Ldloc, then it changes Ldloc to Ldloca and
`Vector2 op_Addition(Vector2,Vector2)` to `Vector2 Add(Vector2,ref Vector2)`.

The following code where r,a,b are Vector2:
 r = a + b;
is transformed to a call as in:
 r = Vector2.Add(a, ref b);

* How to build/test

1) Build TestLib, TestApp, PostOpt

2) run: TestApp\GenIL.bat
or run: PostOpt.exe path\to\testApp.exe

3) run: TestApp\RunTest.bat

* test results

```
P:\PostOpt.Net\TestApp>bin\Release\testApp.exe
op_Addition: 157,1063ms
Add(ref,ref,out): 12,8784ms
Add(ref,ref): 77,0893ms
Add(val,ref): 138,3095ms
TestMethodAddOp(): 155,6869ms
TestMethodAddOp(v,v): 156,1745ms
TestMethodAddOp(v,ref v): 102,8686ms
TestMethodAddOp(v,ref v,out v): 101,0269ms
TestMethodAddRefs(): 23,8763ms
TestMethodAdd_vrv(): 17,5414ms

P:\PostOpt.Net\TestApp>bin\Release\testApp.optmzd.exe
op_Addition: 12,507ms
Add(ref,ref,out): 12,5508ms
Add(ref,ref): 11,9764ms
Add(val,ref): 11,9409ms
TestMethodAddOp(): 29,0932ms
TestMethodAddOp(v,v): 76,4452ms
TestMethodAddOp(v,ref v): 157,4084ms
TestMethodAddOp(v,ref v,out v): 102,9932ms
TestMethodAddRefs(): 20,7105ms
TestMethodAdd_vrv(): 16,9273ms
```
