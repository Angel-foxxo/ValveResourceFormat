/**
 * C# Port of https://github.com/zeux/meshoptimizer/blob/master/src/vertexcodec.cpp
 */
using System;
using System.Buffers;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace ValveResourceFormat.Compression
{
    public static class MeshOptimizerVertexDecoder
    {
        private const byte VertexHeader = 0xa0;

        private const int VertexBlockSizeBytes = 8192;
        private const int VertexBlockMaxSize = 256;
        private const int ByteGroupSize = 16;
        private const int ByteGroupDecodeLimit = 24;
        private const int TailMaxSize = 32;

        private static int GetVertexBlockSize(int vertexSize)
        {
            var result = VertexBlockSizeBytes / vertexSize;
            result &= ~(ByteGroupSize - 1);

            return result < VertexBlockMaxSize ? result : VertexBlockMaxSize;
        }

        private static byte Unzigzag8(byte v)
        {
            return (byte)(-(v & 1) ^ (v >> 1));
        }

        private static readonly byte[][] DecodeBytesGroupShuffle = new byte[256][];
        private static readonly byte[] DecodeBytesGroupCount = new byte[256];

        static MeshOptimizerVertexDecoder()
        {
            for (var mask = 0; mask < 256; mask++)
            {
                var shuffle = new byte[8];
                byte count = 0;

                for (var i = 0; i < 8; i++)
                {
                    var maski = (mask >> i) & 1;
                    shuffle[i] = maski != 0 ? count : (byte)0x80;
                    count += (byte)maski;
                }

                DecodeBytesGroupShuffle[mask] = shuffle;
                DecodeBytesGroupCount[mask] = count;
            }
        }

        private static Vector128<byte> DecodeShuffleMask(byte mask0, byte mask1)
        {
            var m0 = DecodeBytesGroupShuffle[mask0];
            var m1 = DecodeBytesGroupShuffle[mask1];
            var sm0 = Vector128.Create(m0[0], m0[1], m0[2], m0[3], m0[4], m0[5], m0[6], m0[7], 0, 0, 0, 0, 0, 0, 0, 0);
            var sm1 = Vector128.Create(m1[0], m1[1], m1[2], m1[3], m1[4], m1[5], m1[6], m1[7], 0, 0, 0, 0, 0, 0, 0, 0);
            var sm1off = Vector128.Create(DecodeBytesGroupCount[mask0]);

            var sm1r = Sse2.Add(sm1, sm1off);

            return Sse2.UnpackLow(sm0.AsInt64(), sm1r.AsInt64()).AsByte();
        }

        private static Span<byte> DecodeBytesGroupSimd(Span<byte> data, Span<byte> destination, int bitslog2)
        {
            switch (bitslog2)
            {
                case 0:
                    for (var k = 0; k < ByteGroupSize; k++)
                    {
                        destination[k] = 0;
                    }

                    return data;
                case 1:
                    {
                        var sel2 = Vector128.Create(data[0], data[1], data[2], data[3], 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
                        var rest = Vector128.Create(data[4], data[5], data[6], data[7], data[8], data[9], data[10], data[11], data[12], data[13], data[14], data[15], data[16], data[17], data[18], data[19]);

                        var sel22 = Sse2.UnpackLow(Sse2.ShiftRightLogical(sel2.AsInt16(), 4).AsByte(), sel2);
                        var sel2222 = Sse2.UnpackLow(Sse2.ShiftRightLogical(sel22.AsInt16(), 2).AsByte(), sel22);
                        var sel = Sse2.And(sel2222, Vector128.Create((byte)3));

                        var mask = Sse2.CompareEqual(sel, Vector128.Create((byte)3));
                        var mask16 = Sse2.MoveMask(mask);
                        var mask0 = (byte)(mask16 & 255);
                        var mask1 = (byte)(mask16 >> 8);

                        var shuf = DecodeShuffleMask(mask0, mask1);
                        var result = Sse2.Or(Ssse3.Shuffle(rest, shuf), Sse2.AndNot(mask, sel));

                        Vector128.TryCopyTo(result, destination);

                        return data[(4 + DecodeBytesGroupCount[mask0] + DecodeBytesGroupCount[mask1])..];
                    }
                case 2:
                    {
                        var sel4 = Vector128.Create(data[0], data[1], data[2], data[3], data[4], data[5], data[6], data[7], 0, 0, 0, 0, 0, 0, 0, 0);
                        var rest = Vector128.Create(data[8], data[9], data[10], data[11], data[12], data[13], data[14], data[15], data[16], data[17], data[18], data[19], data[20], data[21], data[22], data[23]);

                        var sel44 = Sse2.UnpackLow(Sse2.ShiftRightLogical(sel4.AsInt16(), 4).AsByte(), sel4);
                        var sel = Sse2.And(sel44, Vector128.Create((byte)15));

                        var mask = Sse2.CompareEqual(sel, Vector128.Create((byte)15));
                        var mask16 = Sse2.MoveMask(mask);
                        var mask0 = (byte)(mask16 & 255);
                        var mask1 = (byte)(mask16 >> 8);

                        var shuf = DecodeShuffleMask(mask0, mask1);
                        var result = Sse2.Or(Ssse3.Shuffle(rest, shuf), Sse2.AndNot(mask, sel));

                        Vector128.TryCopyTo(result, destination);

                        return data[(8 + DecodeBytesGroupCount[mask0] + DecodeBytesGroupCount[mask1])..];
                    }
                case 3:
                    data[..ByteGroupSize].CopyTo(destination);

                    return data[ByteGroupSize..];
                default:
                    throw new ArgumentException("Unexpected bit length");
            }
        }

        private static Span<byte> DecodeBytesGroup(Span<byte> data, Span<byte> destination, int bitslog2)
        {
            int dataVar;
            byte b;
            byte enc;

            byte Next(byte bits, byte encv)
            {
                enc = b;
                enc >>= 8 - bits;
                b <<= bits;

                if (enc == (1 << bits) - 1)
                {
                    dataVar += 1;
                    return encv;
                }

                return enc;
            }

            switch (bitslog2)
            {
                case 0:
                    for (var k = 0; k < ByteGroupSize; k++)
                    {
                        destination[k] = 0;
                    }

                    return data;
                case 1:
                    dataVar = 4;

                    b = data[0];
                    destination[0] = Next(2, data[dataVar]);
                    destination[1] = Next(2, data[dataVar]);
                    destination[2] = Next(2, data[dataVar]);
                    destination[3] = Next(2, data[dataVar]);

                    b = data[1];
                    destination[4] = Next(2, data[dataVar]);
                    destination[5] = Next(2, data[dataVar]);
                    destination[6] = Next(2, data[dataVar]);
                    destination[7] = Next(2, data[dataVar]);

                    b = data[2];
                    destination[8] = Next(2, data[dataVar]);
                    destination[9] = Next(2, data[dataVar]);
                    destination[10] = Next(2, data[dataVar]);
                    destination[11] = Next(2, data[dataVar]);

                    b = data[3];
                    destination[12] = Next(2, data[dataVar]);
                    destination[13] = Next(2, data[dataVar]);
                    destination[14] = Next(2, data[dataVar]);
                    destination[15] = Next(2, data[dataVar]);

                    return data[dataVar..];
                case 2:
                    dataVar = 8;

                    b = data[0];
                    destination[0] = Next(4, data[dataVar]);
                    destination[1] = Next(4, data[dataVar]);

                    b = data[1];
                    destination[2] = Next(4, data[dataVar]);
                    destination[3] = Next(4, data[dataVar]);

                    b = data[2];
                    destination[4] = Next(4, data[dataVar]);
                    destination[5] = Next(4, data[dataVar]);

                    b = data[3];
                    destination[6] = Next(4, data[dataVar]);
                    destination[7] = Next(4, data[dataVar]);

                    b = data[4];
                    destination[8] = Next(4, data[dataVar]);
                    destination[9] = Next(4, data[dataVar]);

                    b = data[5];
                    destination[10] = Next(4, data[dataVar]);
                    destination[11] = Next(4, data[dataVar]);

                    b = data[6];
                    destination[12] = Next(4, data[dataVar]);
                    destination[13] = Next(4, data[dataVar]);

                    b = data[7];
                    destination[14] = Next(4, data[dataVar]);
                    destination[15] = Next(4, data[dataVar]);

                    return data[dataVar..];
                case 3:
                    data[..ByteGroupSize].CopyTo(destination);

                    return data[ByteGroupSize..];
                default:
                    throw new ArgumentException("Unexpected bit length");
            }
        }


        private static Span<byte> DecodeBytesSimd(Span<byte> data, Span<byte> destination)
        {
            if (destination.Length % ByteGroupSize != 0)
            {
                throw new ArgumentException("Expected data length to be a multiple of ByteGroupSize.");
            }

            var headerSize = ((destination.Length / ByteGroupSize) + 3) / 4;
            var header = data[..];

            data = data[headerSize..];

            int i = 0;

            // fast-path: process 4 groups at a time, do a shared bounds check - each group reads <=24b
            for (; i + ByteGroupSize * 4 <= destination.Length && data.Length >= ByteGroupDecodeLimit * 4; i += ByteGroupSize * 4)
            {
                var header_offset = i / ByteGroupSize;
                var header_byte = header[header_offset / 4];

                data = DecodeBytesGroupSimd(data, destination[(i + ByteGroupSize * 0)..], (header_byte >> 0) & 3);
                data = DecodeBytesGroupSimd(data, destination[(i + ByteGroupSize * 1)..], (header_byte >> 2) & 3);
                data = DecodeBytesGroupSimd(data, destination[(i + ByteGroupSize * 2)..], (header_byte >> 4) & 3);
                data = DecodeBytesGroupSimd(data, destination[(i + ByteGroupSize * 3)..], (header_byte >> 6) & 3);
            }

            // slow-path: process remaining groups
            for (; i < destination.Length; i += ByteGroupSize)
            {
                if (data.Length < TailMaxSize)
                {
                    throw new InvalidOperationException("Cannot decode");
                }

                var headerOffset = i / ByteGroupSize;

                var bitslog2 = (header[headerOffset / 4] >> (headerOffset % 4 * 2)) & 3;

                data = DecodeBytesGroupSimd(data, destination[i..], bitslog2);
            }

            return data;
        }

        private static Span<byte> DecodeBytes(Span<byte> data, Span<byte> destination)
        {
            if (destination.Length % ByteGroupSize != 0)
            {
                throw new ArgumentException("Expected data length to be a multiple of ByteGroupSize.");
            }

            var headerSize = ((destination.Length / ByteGroupSize) + 3) / 4;
            var header = data[..];

            data = data[headerSize..];

            for (var i = 0; i < destination.Length; i += ByteGroupSize)
            {
                if (data.Length < TailMaxSize)
                {
                    throw new InvalidOperationException("Cannot decode");
                }

                var headerOffset = i / ByteGroupSize;

                var bitslog2 = (header[headerOffset / 4] >> (headerOffset % 4 * 2)) & 3;

                data = DecodeBytesGroup(data, destination[i..], bitslog2);
            }

            return data;
        }

        private static Span<byte> DecodeVertexBlock(Span<byte> data, Span<byte> vertexData, int vertexCount, int vertexSize, Span<byte> lastVertex)
        {
            if (vertexCount <= 0 || vertexCount > VertexBlockMaxSize)
            {
                throw new ArgumentException("Expected vertexCount to be between 0 and VertexMaxBlockSize");
            }

            var bufferPool = ArrayPool<byte>.Shared.Rent(VertexBlockMaxSize);
            var buffer = bufferPool.AsSpan(0, VertexBlockMaxSize);
            var transposedPool = ArrayPool<byte>.Shared.Rent(VertexBlockSizeBytes);
            var transposed = transposedPool.AsSpan(0, VertexBlockSizeBytes);

            try
            {
                var vertexCountAligned = (vertexCount + ByteGroupSize - 1) & ~(ByteGroupSize - 1);

                for (var k = 0; k < vertexSize; ++k)
                {
                    //data = DecodeBytes(data, buffer[..vertexCountAligned]);
                    data = DecodeBytesSimd(data, buffer[..vertexCountAligned]);

                    var vertexOffset = k;

                    var p = lastVertex[k];

                    for (var i = 0; i < vertexCount; ++i)
                    {
                        var v = (byte)(Unzigzag8(buffer[i]) + p);

                        transposed[vertexOffset] = v;
                        p = v;

                        vertexOffset += vertexSize;
                    }
                }

                transposed[..(vertexCount * vertexSize)].CopyTo(vertexData);

                transposed.Slice(vertexSize * (vertexCount - 1), vertexSize).CopyTo(lastVertex);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(bufferPool);
                ArrayPool<byte>.Shared.Return(transposedPool);
            }

            return data;
        }

        public static byte[] DecodeVertexBuffer(int vertexCount, int vertexSize, byte[] vertexBuffer)
        {
            if (vertexSize <= 0 || vertexSize > 256)
            {
                throw new ArgumentException("Vertex size is expected to be between 1 and 256");
            }

            if (vertexSize % 4 != 0)
            {
                throw new ArgumentException("Vertex size is expected to be a multiple of 4.");
            }

            if (vertexBuffer.Length < 1 + vertexSize)
            {
                throw new ArgumentException("Vertex buffer is too short.");
            }

            var vertexSpan = new Span<byte>(vertexBuffer);

            var header = vertexSpan[0];
            vertexSpan = vertexSpan[1..];
            if (header != VertexHeader)
            {
                throw new ArgumentException($"Invalid vertex buffer header, expected {VertexHeader} but got {header}.");
            }

            var lastVertex = new byte[vertexSize];
            vertexSpan.Slice(vertexBuffer.Length - 1 - vertexSize, vertexSize).CopyTo(lastVertex);

            var vertexBlockSize = GetVertexBlockSize(vertexSize);

            var vertexOffset = 0;

            var result = new Span<byte>(new byte[vertexCount * vertexSize]);

            while (vertexOffset < vertexCount)
            {
                var blockSize = vertexOffset + vertexBlockSize < vertexCount
                    ? vertexBlockSize
                    : vertexCount - vertexOffset;

                vertexSpan = DecodeVertexBlock(vertexSpan, result[(vertexOffset * vertexSize)..], blockSize, vertexSize, lastVertex);

                vertexOffset += blockSize;
            }

            return result.ToArray();
        }
    }
}
