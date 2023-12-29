using lightningExceptions;
using lightningTools;
using lightningUnit;

namespace lightningVM
{
    public struct Stack
    {
        Unit[] values;
        public int top;
        Unit[] stash;
        int stashTop;

        public Stack(int p_callStackSize)
        {
            values = new Unit[3 * p_callStackSize];
            top = 0;
            stash = new Unit[p_callStackSize];
            stashTop = 0;
        }

        public void Clear()
        {
            top = 0;
            stashTop = 0;
        }

        public void Push(Unit p_value)
        {
            values[top] = p_value;
            top++;
        }

        public Unit Pop()
        {
            top--;
            Unit popped = values[top];
            return popped;
        }

        public Unit Peek()
        {
            return values[top - 1];
        }

        public Unit Peek(int p_n)
        {
            if ((p_n < 0) || (p_n > (top - 1)))
            {
                Logger.Log("Atempt to read empty stack.", Defaults.Config.VMLogFile);
                throw Exceptions.empty_stack;
            }
            return values[top - p_n - 1];
        }

        public void PushStash()
        {
            stash[stashTop] = Pop();
            stashTop++;
        }

        public void PopStash()
        {
            Push(stash[stashTop - 1]);
            stashTop--;
        }
    }
}