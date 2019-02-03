# PostOpt.Net
A post build tool to  weave your assemblies for better performance.


*This tool is currently in a proof of concept / expirimental stage.*

PostOpt.exe will search all methods in the assembly for a call to Vector2.op_Addition().
If the intstruction before op_Addition is Ldloc, then it changes Ldloc to Ldloca and
`Vector2 op_Addition(Vector2,Vector2)` to `Vector2 Add(Vector2,ref Vector2)`.

The following code where r,a,b are Vector2:
 r = a + b;
is transformed to a call as in:
 r = Add(a, ref b);

* How to build/test

1) Build TestLib, TestApp, PostOpt

2) run: TestApp\GenIL.bat
or run: PostOpt.exe path\to\testApp.exe

3) run: TestApp\RunTest.bat

* test results

```
P:\PostOpt.Net\TestApp>bin\Release\testApp.exe
op_Addition: 77,1343ms
Add(ref,ref,out): 6,102ms
Add(valuetype, ref): 72,8027ms
TestMethodAddOp(): 152,4396ms
TestMethodAddRefs(): 17,1742ms
TestMethodAdd_vrv(): 22,7803ms

P:\PostOpt.Net\TestApp>bin\Release\testApp.optmzd.exe
op_Addition: 9,6794ms
Add(ref,ref,out): 7,1842ms
Add(valuetype, ref): 71,9009ms
TestMethodAddOp(): 88,9036ms
TestMethodAddRefs(): 19,5572ms
TestMethodAdd_vrv(): 19,4596ms
```
