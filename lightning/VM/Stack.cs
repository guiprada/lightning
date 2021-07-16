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

        public void Clear(){
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

        public Unit Peek(int n)
        {
            if (n < 0 || n > (top - 1)){
                throw new Exception("Atempt to read empty stack. " + VM.ErrorString(null));
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