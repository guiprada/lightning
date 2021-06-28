using System;
namespace lightning{
	public struct Stack
	{
		Unit[] values;
		public int top;

		public Stack(int p_size)
		{
			values = new Unit[p_size];
			top = 0;
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
				throw new Exception("Atempt to read empty stack");
			}
			return values[top - n - 1];
		}
	}
}