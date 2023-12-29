using System;

using lightningExceptions;
using lightningTools;

namespace lightningUnit
{
	public class WrapperUnit<T> : HeapUnit
    {
        public object content;
        public TableUnit ExtensionTable { get; private set; }
        public override UnitType Type
        {
            get
            {
                return UnitType.Wrapper;
            }
        }
        public WrapperUnit(object p_content, TableUnit p_ExtentionTable = null)
        {
            content = p_content;
            ExtensionTable = p_ExtentionTable;
        }

        public override Unit Get(Unit p_key)
        {
            return ExtensionTable.Get(p_key);
        }

        public override string ToString()
        {
            return content.ToString();
        }

        public override bool Equals(object p_other)
        {
            Type other_type = p_other.GetType();
            if (other_type == typeof(Unit))
            {
                if (((Unit)p_other).Type == UnitType.Wrapper)
                {
                    if (this.content == (((Unit)p_other).heapUnitValue as WrapperUnit<T>).content) return true;
                }
            }
            if (other_type == typeof(WrapperUnit<T>))
            {
                if (((WrapperUnit<T>)p_other).content == this.content)
                {
                    return true;
                }
            }
            return false;
        }

        public override int GetHashCode()
        {
            return content.GetHashCode();
        }
        public T UnWrap()
        {
            if (this.content.GetType() == typeof(T))
            {
                return (T)content;
            }
            else
            {
                Logger.Log("UnWrap<" + typeof(T) + ">() type Error!", Defaults.Config.VMLogFile);
                throw Exceptions.wrong_type;
            }
        }

        public override void SetExtensionTable(TableUnit p_ExtensionTable)
        {
            ExtensionTable = p_ExtensionTable;
        }

        public override void UnsetExtensionTable()
        {
            ExtensionTable = null;
        }

        public override TableUnit GetExtensionTable()
        {
            return ExtensionTable;
        }
    }
}