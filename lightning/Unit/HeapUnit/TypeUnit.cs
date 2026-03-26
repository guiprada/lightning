namespace lightningUnit
{
	public class TypeUnit : HeapUnit
    {
        // Plain singletons (no protection)
        public static readonly TypeUnit Float        = new TypeUnit(UnitType.Float,    0);
        public static readonly TypeUnit Integer      = new TypeUnit(UnitType.Integer,  0);
        public static readonly TypeUnit Boolean      = new TypeUnit(UnitType.Boolean,  0);
        public static readonly TypeUnit Char         = new TypeUnit(UnitType.Char,     0);
        public static readonly TypeUnit Void         = new TypeUnit(UnitType.Void,     0);

        // Const singletons — same UnitType, PROTECTION_CONST flag set.
        // Used by MAKE_CONST and DECLARE_CONST_VARIABLE to represent const scalars
        // without touching the Unit struct's protectionFlags field (which overlaps
        // the numeric value in DOUBLE mode).
        public static readonly TypeUnit ConstFloat   = new TypeUnit(UnitType.Float,    Unit.PROTECTION_CONST);
        public static readonly TypeUnit ConstInteger = new TypeUnit(UnitType.Integer,  Unit.PROTECTION_CONST);
        public static readonly TypeUnit ConstBoolean = new TypeUnit(UnitType.Boolean,  Unit.PROTECTION_CONST);
        public static readonly TypeUnit ConstChar    = new TypeUnit(UnitType.Char,     Unit.PROTECTION_CONST);

        UnitType type;
        public readonly int ProtectionFlags;

        public override UnitType Type
        {
            get
            {
                return type;
            }
        }

        private TypeUnit(UnitType p_type, int p_protectionFlags)
        {
            type = p_type;
            ProtectionFlags = p_protectionFlags;
        }

        public override string ToString()
        {
            if (this.type == UnitType.Float)
                return "UnitType.Float";
            else if (this.type == UnitType.Integer)
                return "UnitType.Integer";
            else if (this.type == UnitType.Boolean)
                return "UnitType.Boolean";
            else if (this.type == UnitType.Char)
                return "UnitType.Char";
            else if (this.type == UnitType.Result)
                return "UnitType.Result";
            else if (this.type == UnitType.Void)
                return "UnitType.Void";
            else
                return "Unknown UnitType";
        }

        public override bool Equals(object p_other)
        {
            if (p_other.GetType() == typeof(TypeUnit))
                return this.type == ((TypeUnit)p_other).type;
            return false;
        }
        public override int GetHashCode()
        {
            return this.type.GetHashCode();
        }
    }
}