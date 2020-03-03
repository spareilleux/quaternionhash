using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Numerics;

namespace QuaternionHash
{
    public class QuaternionHashFactory
    {
        private readonly IImmutableDictionary<char, Quaternion> _quaternionByChar;
        private readonly int _segmentIndexShift;

        public QuaternionHashFactory(int segmentIndexShift = 0)
        {
            _segmentIndexShift = segmentIndexShift;
            _quaternionByChar = GetQuaternionByChar();
        }

        public Quaternion CreateHash(string s)
        {
            var result =
                s.Select(c => _quaternionByChar[c])
                 .Aggregate(Quaternion.Identity, (q, cq) => q * cq);

            result = Quaternion.Normalize(result);

            return result;
        }

        private IImmutableDictionary<char, Quaternion> GetQuaternionByChar()
        {
            const char min = char.MinValue;
            const char max = char.MaxValue;
            var charByQuaternion = new Dictionary<char, Quaternion>();
            var c = min;
            while (true)
            {
                c++;
                if (c == max) break;
                charByQuaternion.Add(c, GetQuaternion(c));
            }

            var result = charByQuaternion.ToImmutableDictionary();

            return result;
        }

        private Quaternion GetQuaternion(char c)
        {
            var cBytes = BitConverter.GetBytes(c);
            var hashBytes = BitConverter.GetBytes(c.GetHashCode());

            var bytes = new List<byte>();
            bytes.AddRange(cBytes);
            bytes.AddRange(hashBytes);
            var byteArray = bytes.ToArray();
            var bits = new BitArray(byteArray);

            // Create segments
            const int segmentCount = 4;
            var segmentLength = bits.Count / segmentCount;
            var segments = new List<BitArray>();
            for (var i = 0; i < segmentCount; i++)
            {
                var segment = new BitArray(segmentLength);
                segments.Add(segment);
            }

            var segmentArray = segments.ToImmutableArray();

            // Dispatch bits to segment
            for (var i = 0; i < bits.Count - 1; i++)
            {
                // Get segment
                var targetSegmentIndex = (i + _segmentIndexShift) % segmentCount;
                var targetSegmentBitIndex = i / segmentCount;
                var segment = segmentArray[targetSegmentIndex];

                // Add bit to segment
                var bit = bits[i];
                if (targetSegmentBitIndex > 0) segment = segment.LeftShift(1);
                segment[0] = bit;
            }

            var xBits = segmentArray[0];
            var yBits = segmentArray[1];
            var zBits = segmentArray[2];
            var wBits = segmentArray[3];

            var xArray = new int[1];
            xBits.CopyTo(xArray, 0);
            var x = xArray.First();

            var yArray = new int[1];
            yBits.CopyTo(yArray, 0);
            var y = yArray.First();

            var zArray = new int[1];
            zBits.CopyTo(zArray, 0);
            var z = zArray.First();

            var wArray = new int[1];
            wBits.CopyTo(wArray, 0);
            var w = wArray.First();

            var result = new Quaternion(x, y, z, w);
            result = Quaternion.Normalize(result);

            return result;
        }
    }
}