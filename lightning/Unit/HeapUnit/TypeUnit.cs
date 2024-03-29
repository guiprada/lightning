namespace lightningUnit
{
	public class TypeUnit : HeapUnit
    {
        public static TypeUnit Float = new TypeUnit(UnitType.Float);
        public static TypeUnit Integer = new TypeUnit(UnitType.Integer);
        public static TypeUnit Boolean = new TypeUnit(UnitType.Boolean);
        public static TypeUnit Char = new TypeUnit(UnitType.Char);
        public static TypeUnit Void = new TypeUnit(UnitType.Void);

        UnitType type;

        public override UnitType Type
        {
            get
            {
                return type;
            }
        }

        private TypeUnit(UnitType p_type)
        {
            type = p_type;
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