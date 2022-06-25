using System;
using System.Runtime.CompilerServices;
using System.Threading;

public class Helpers {

    public void IterateByteArray(byte[][] target, Action<byte[]> operation) {
        foreach (byte[] ByteArray in target) {
            operation.Invoke(ByteArray);
        }
    }

    public long GetByteArraySizes(byte[][] target) {
        long Size = 0;
        //  IterateByteArray(target, Sub(ByteArray) Size += ByteArray.Length)
        foreach (byte[] ByteArray in target) {
            Size = (Size + ByteArray.Length);
        }

        return Size;
    }

    public byte[] CombineByteArrays(byte[][] target) {
        long Size = GetByteArraySizes(target);
        byte[] Merged = new byte[Size];
        int Index = 0;
        foreach (byte[] ByteArray in target) {
            ByteArray.CopyTo(Merged, Index);
            Index = (Index + ByteArray.Length);
        }

        return Merged;
    }
}