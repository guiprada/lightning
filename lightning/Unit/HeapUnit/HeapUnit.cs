using System;
using System.Collections.Generic;

using lightningVM;
namespace lightningUnit
{
    public abstract class HeapUnit
    {
        public abstract UnitType Type { get; }
        public abstract override string ToString();
        public virtual bool ToBool()
        {
            throw new Exception("Can not convert " + Type + " to Bool.");
        }
        public abstract override bool Equals(object other);
        public abstract override int GetHashCode();
        public virtual Unit Get(Unit p_key)
        {
            throw new Exception("Trying to Get a Table value of " + Type);
        }
        public virtual void Set(Unit index, Unit value)
        {
            throw new Exception("Trying to Set a Table value of " + Type);
        }
        public virtual void SetExtensionTable(TableUnit p_ExtensionTable)
        {
            throw new Exception("Trying to set a Extension Table of a " + Type);
        }
        public virtual void UnsetExtensionTable()
        {
            throw new Exception("Trying to unset a Extension Table of a " + Type);
        }
        public virtual TableUnit GetExtensionTable()
        {
            throw new Exception("Trying to get a Extension Table of a " + Type);
        }
        public virtual int CompareTo(object p_compareTo)
        {
            throw new Exception("Trying to compare a " + Type);
        }
    }
}
