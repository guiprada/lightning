using System;
using System.Collections.Generic;

using lightningExceptions;
using lightningTools;
using lightningVM;

namespace lightningUnit
{
    public abstract class HeapUnit
    {
        public abstract UnitType Type { get; }
        public abstract override string ToString();
        public virtual bool ToBool()
        {
            Logger.Log("Can not convert " + Type + " to Bool.", Defaults.Config.VMLogFile);
            throw Exceptions.can_not_convert;
        }
        public abstract override bool Equals(object other);
        public abstract override int GetHashCode();
        public virtual Unit Get(Unit p_key)
        {
            Logger.Log("Trying to Get a Table value of " + Type, Defaults.Config.VMLogFile);
            throw Exceptions.not_supported;
        }
        public virtual void Set(Unit index, Unit value)
        {
            Logger.Log("Trying to Set a Table value of " + Type, Defaults.Config.VMLogFile);
            throw Exceptions.not_supported;
        }
        public virtual void SetExtensionTable(TableUnit p_ExtensionTable)
        {
            Logger.Log("Trying to set a Extension Table of a " + Type, Defaults.Config.VMLogFile);
            throw Exceptions.not_supported;
        }
        public virtual void UnsetExtensionTable()
        {
            Logger.Log("Trying to unset a Extension Table of a " + Type, Defaults.Config.VMLogFile);
            throw Exceptions.not_supported;
        }
        public virtual TableUnit GetExtensionTable()
        {
            Logger.Log("Trying to get a Extension Table of a " + Type, Defaults.Config.VMLogFile);
            throw Exceptions.not_supported;
        }
        public virtual int CompareTo(object p_compareTo)
        {
            Logger.Log("Trying to compare a " + Type, Defaults.Config.VMLogFile);
            throw Exceptions.can_not_compare;
        }
    }
}
