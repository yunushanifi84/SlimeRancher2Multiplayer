namespace System.Runtime.CompilerServices
{
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    internal sealed class NullableAttribute : Attribute
    {
        public readonly byte[] NullableFlags;
        public NullableAttribute(byte A_0) { NullableFlags = new byte[] { A_0 }; }
        public NullableAttribute(byte[] A_0) { NullableFlags = A_0; }
    }
    [AttributeUsage(AttributeTargets.All, AllowMultiple = false)]
    internal sealed class NullableContextAttribute : Attribute
    {
        public readonly byte Flag;
        public NullableContextAttribute(byte A_0) { Flag = A_0; }
    }
}