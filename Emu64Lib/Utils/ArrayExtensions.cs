
namespace C64Lib.Utils
{
    public static class ArrayExtensions
    {
        // requires destination to be pre-sized
        public static int CopyFrom<T>(this T[] destination, T[] source, int sourceStartIndex)
        {
            for (int i = 0; i < destination.Length; i++)
            {
                destination[i] = source[sourceStartIndex++];
            }

            return sourceStartIndex;
        }


        // requires destination to be pre-sized
        public static int CopyFrom(this sbyte[] destination, byte[] source, int sourceStartIndex)
        {
            for (int i = 0; i < destination.Length; i++)
            {
                destination[i] = (sbyte)source[sourceStartIndex++];
            }

            return sourceStartIndex;
        }


        public static byte[] CopySubset(this byte[] source, int sourceStartIndex)
        {
            byte[] destination = new byte[source.Length - sourceStartIndex];

            destination.CopyFrom(source, sourceStartIndex);

            return destination;

        }

    }
}
