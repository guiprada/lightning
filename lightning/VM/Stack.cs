using System;
namespace lightning{
    public struct Stack
    {
        Unit[] values;
        public int top;
        Unit[] stash;
        int stashTop;

        public Stack(int p_function_deepness)
        {
            values = new Unit[3 * p_function_deepness];
            top = 0;
            stash = new Unit[p_function_deepness];
            stashTop = 0;
        }

        public void Push(Unit p_value)
        {
            values[top] = p_value;
            top++;
            if(p_value.GetType() == typeof(NumberUnit))
                ((NumberUnit)p_value).stacked = true;
        }

        public Unit Pop()
        {
            top--;
            Unit popped = values[top];
            if(popped.GetType() == typeof(NumberUnit))
                ((NumberUnit)popped).stacked = false;
            return popped;
        }

        public Unit Peek()
        {
            return values[top - 1];
        }

        public Unit Peek(int n)
        {
            if (n < 0 || n > (top - 1)){
                throw new Exception("Atempt to read empty stack");
            }
            return values[top - n - 1];
        }

        public void PushStash(){
            stash[stashTop] = Pop();
            stashTop++;
        }

        public void PopStash(){
            Push(stash[stashTop - 1]);
            stashTop--;
        }
    }
}