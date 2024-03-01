namespace SFSMultiPlayer
{
    public class DoubleValueTable<T1, T2>
    {
        T1 value1;
        T2 value2;

        public DoubleValueTable(T1 value1, T2 value2)
        {
            this.value1 = value1;
            this.value2 = value2;
        }

        public T1 Value1
        {
            get
            {
                return value1;
            }
            set
            {
                value1 = value;
            }
        }

        public T2 Value2
        {
            get
            {
                return value2;
            }
            set
            {
                value2 = value;
            }
        }
    }
}
