using System;
using System.Collections.Generic;

#if DOUBLE
    using Number = System.Double;
    using Integer = System.Int64;
#else
    using Number = System.Single;
    using Integer = System.Int32;
#endif

namespace lightning
{
    public class NumberPool{
        Stack<NumberUnit> objects;
        uint maxUsed;
        uint inUse;
        uint recycled;

        public int Count{
            get{
                return objects.Count;
            }
        }

        public int MaxUsed{
            get{
                return (int)maxUsed;
            }
        }

        public int InUse{
            get{
                return (int)inUse;
            }
        }

        public int Recycled{
            get{
                return (int)recycled;
            }
        }
        public NumberPool(){
            objects = new Stack<NumberUnit>();
            maxUsed = 0;
            inUse = 0;
            recycled = 0;
        }

        public NumberUnit Get(Unit p_unit){
            if(p_unit.GetType() != typeof(NumberUnit))
                throw new Exception("Trying to Get a non NumberUnit from NumberPool");
            return Get(((NumberUnit)p_unit).content);
        }

        public NumberUnit Get(Number n){
            inUse++;
            if(inUse > maxUsed)
                maxUsed = inUse;

            if (objects.Count > 0)
            {
                NumberUnit v = objects.Pop();
                v.content = n;
                return v;
            }
            else
            {
                return new NumberUnit(n);
            }
        }

        public void Release(Unit p_unit){
            if(p_unit.GetType() != typeof(NumberUnit))
                throw new Exception("Trying to Get a non NumberUnit from NumberPool");
            Release((NumberUnit)p_unit);
        }

        public void Release(NumberUnit v)
        {
            if(v.referenced == false && v.stacked == false){
                recycled++;
                inUse--;
                if(objects.Count <= maxUsed)
                    objects.Push(v);
            }
        }
    }
}