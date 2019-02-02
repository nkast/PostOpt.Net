using System;

namespace Xna.Framework
{
    public struct Vector2
    {
        public float X;
        public float Y;

        public Vector2(float x, float y)
        {
            this.X = x;
            this.Y = y;
        }

        public static Vector2 operator +(Vector2 a, Vector2 b)
        {
            a.X += b.X;
            a.Y += b.Y;
            return a;
        }

        public static void Add(ref Vector2 a, ref Vector2 b, out Vector2 r)
        {
            r.X = a.X + b.X;
            r.Y = a.Y + b.Y;
        }
                
        public static Vector2 Add_vvv(Vector2 a, Vector2 b)
        {
            Vector2 r;
            r.X = a.X + b.X;
            r.Y = a.Y + b.Y;            
            return r;
        }

        public static Vector2 Add_rvv(ref Vector2 a, Vector2 b)
        {
            Vector2 r;
            r.X = a.X + b.X;
            r.Y = a.Y + b.Y;
            return r;
        }
                
        public static Vector2 Add_vrv(Vector2 a, ref Vector2 b)
        {
            Vector2 r;
            r.X = a.X + b.X;
            r.Y = a.Y + b.Y;
            return r;
        }
                
        public static Vector2 Add_rrv(ref Vector2 a, ref Vector2 b)
        {
            Vector2 r;
            r.X = a.X + b.X;
            r.Y = a.Y + b.Y;
            return r;
        }
                 
        public static void Add4(Vector2 a, Vector2 b, out Vector2 r)
        {
            r.X = a.X + b.X;
            r.Y = a.Y + b.Y;
        }
    }
}
