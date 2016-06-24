namespace Elpis.Kayak
{
    internal class OutputBuffer
    {
        public OutputBuffer()
        {
            Data = new System.Collections.Generic.List<System.ArraySegment<byte>>();
        }

        public System.Collections.Generic.List<System.ArraySegment<byte>> Data;
        public int Size;

        public void Add(System.ArraySegment<byte> data)
        {
            byte[] d = new byte[data.Count];
            System.Buffer.BlockCopy(data.Array, data.Offset, d, 0, d.Length);

            Size += data.Count;
            Data.Add(new System.ArraySegment<byte>(d));
        }

        public void Remove(int howmuch)
        {
            if (howmuch > Size) throw new System.ArgumentOutOfRangeException("howmuch > size");

            Size -= howmuch;

            int remaining = howmuch;

            while (remaining > 0)
            {
                System.ArraySegment<byte> first = Data[0];

                int count = first.Count;
                if (count <= remaining)
                {
                    remaining -= count;
                    Data.RemoveAt(0);
                }
                else
                {
                    Data[0] = new System.ArraySegment<byte>(first.Array, first.Offset + remaining, count - remaining);
                    remaining = 0;
                }
            }
        }
    }
}