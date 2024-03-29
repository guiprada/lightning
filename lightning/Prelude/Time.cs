using System;
using lightningUnit;
using lightningVM;
namespace lightningPrelude
{
    public class Time
    {
        public static TableUnit GetTableUnit()
        {
            TableUnit time = new TableUnit(null);
            TableUnit timeMethods = new TableUnit(null);

            Unit TimeNow(VM p_vm)
            {
                return new Unit(new WrapperUnit<long>(DateTime.Now.Ticks, timeMethods));
            }
            time.Set("now", new IntrinsicUnit("time_now", TimeNow, 0));

            //////////////////////////////////////////////////////
            Unit TimeReset(VM p_vm)
            {
                WrapperUnit<long> this_time = p_vm.GetWrapperUnit<long>(0);
                this_time.content = DateTime.Now.Ticks;
                return new Unit(true);
            }
            timeMethods.Set("reset", new IntrinsicUnit("time_reset", TimeReset, 1));

            //////////////////////////////////////////////////////
            Unit TimeElapsed(VM p_vm)
            {
                long timeStart = p_vm.GetWrappedContent<long>(0);
                long timeEnd = DateTime.Now.Ticks;
                return new Unit((Integer)(new TimeSpan(timeEnd - timeStart).TotalMilliseconds));// Convert to milliseconds
            }
            timeMethods.Set("elapsed", new IntrinsicUnit("time_elapsed", TimeElapsed, 1));

            return time;
        }
    }
}