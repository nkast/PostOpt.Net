using System;
using Xna.Framework;

namespace testApp
{
    public class Test
    {
        public Test()
        {
        }

        public void Run()
        {            
            Vector2 a = new Vector2(3,5);
            Vector2 b = new Vector2(7,11);
            Vector2 r = new Vector2();

            var sw = new System.Diagnostics.Stopwatch();
            //int c;
            
            sw.Start();
            for (int c = 0; c < 10000000; c++)
            {
                r = a + b;
            }
            sw.Stop();
            System.Diagnostics.Trace.WriteLine("op_Addition: " + sw.Elapsed.TotalMilliseconds+"ms");
            System.Console.WriteLine("op_Addition: " + sw.Elapsed.TotalMilliseconds+"ms");

            sw.Reset();
            
            sw.Start();
            for (int c = 0; c < 10000000; c++)
            {
                Vector2.Add(ref a, ref b, out r);
            }
            sw.Stop();
            System.Diagnostics.Trace.WriteLine("Add(ref,ref,out): " + sw.Elapsed.TotalMilliseconds+"ms");
            System.Console.WriteLine("Add(ref,ref,out): " + sw.Elapsed.TotalMilliseconds+"ms");

            sw.Reset();
            
            sw.Start();
            for (int c = 0; c < 10000000; c++)
            {
                r = Vector2.Add_vrv(a, ref b);
            }
            sw.Stop();
            System.Diagnostics.Trace.WriteLine("Add_vrv(valuetype,ref): " + sw.Elapsed.TotalMilliseconds+"ms");
            System.Console.WriteLine("Add_vrv(valuetype, ref): " + sw.Elapsed.TotalMilliseconds+"ms");


            ///////
            
            sw.Start();
            for (int c = 0; c < 10000000; c++)
            {
                r = TestMethodAddOp();
            }
            sw.Stop();
            System.Diagnostics.Trace.WriteLine("TestMethodAddOp(): " + sw.Elapsed.TotalMilliseconds+"ms");
            System.Console.WriteLine("TestMethodAddOp(): " + sw.Elapsed.TotalMilliseconds+"ms");

            sw.Reset();
            
            sw.Start();
            for (int c = 0; c < 10000000; c++)
            {
                r = TestMethodAddRefs();
            }
            sw.Stop();
            System.Diagnostics.Trace.WriteLine("TestMethodAddRefs(): " + sw.Elapsed.TotalMilliseconds+"ms");
            System.Console.WriteLine("TestMethodAddRefs(): " + sw.Elapsed.TotalMilliseconds+"ms");

            sw.Reset();
            
            sw.Start();
            for (int c = 0; c < 10000000; c++)
            {
                r = TestMethodAdd_vrv();
            }
            sw.Stop();
            System.Diagnostics.Trace.WriteLine("TestMethodAdd_vrv(): " + sw.Elapsed.TotalMilliseconds+"ms");
            System.Console.WriteLine("TestMethodAdd_vrv(): " + sw.Elapsed.TotalMilliseconds+"ms");

            var ra = r;
        }

        
        public static Vector2 TestMethodAddOp()
        {
            Vector2 a = new Vector2(3,5);
            Vector2 b = new Vector2(7,11);
            Vector2 r;

            r = a + b;
            return r;
        }


        public static Vector2 TestMethodAddRefs()
        {
            Vector2 a = new Vector2(3,5);
            Vector2 b = new Vector2(7,11);
            Vector2 r;

            Vector2.Add(ref a, ref b, out r);
            return r;
        }
               
        public static Vector2 TestMethodAdd_vvv()
        {
            Vector2 a = new Vector2(3,5);
            Vector2 b = new Vector2(7,11);
            Vector2 r;

            r = Vector2.Add_vvv(a, b);
            return r;
        }
         
        public static Vector2 TestMethodAdd_rvv()
        {
            Vector2 a = new Vector2(3,5);
            Vector2 b = new Vector2(7,11);
            Vector2 r;

            r = Vector2.Add_rvv(ref a, b);
            return r;
        }

        public static Vector2 TestMethodAdd_vrv()
        {
            Vector2 a = new Vector2(3,5);
            Vector2 b = new Vector2(7,11);
            Vector2 r;

            r = Vector2.Add_vrv(a, ref b);
            return r;
        }
                
        public static Vector2 TestMethodAdd_rrv()
        {
            Vector2 a = new Vector2(3,5);
            Vector2 b = new Vector2(7,11);
            Vector2 r;

            r = Vector2.Add_rrv(ref a, ref b);
            return r;
        }  

        public static Vector2 TestMethodAdd_vvo()
        {
            Vector2 a = new Vector2(3,5);
            Vector2 b = new Vector2(7,11);
            Vector2 r;

            Vector2.Add4(a, b, out r);
            return r;
        }
    }
}