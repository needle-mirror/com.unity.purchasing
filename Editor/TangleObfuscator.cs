using System;
using System.Linq;
using System.Collections.Generic;

namespace UnityEditor.Purchasing
{
    public static class TangleObfuscator
    {
        public class InvalidOrderArray : Exception {}

        public static byte[] Obfuscate(byte[] data, int[] order, out int rkey)
        {
            var rnd = new System.Random();
            int key = rnd.Next(2, 255);
            byte[] res = new byte[data.Length];
            int slices = data.Length / 20 + 1;

            if (order == null || order.Length < slices)
            {
				throw new InvalidOrderArray();
			}

            Array.Copy(data, res, data.Length);
            for (int i = 0; i < slices - 1; i ++)
            {
                int j = rnd.Next(i, slices - 1);
                order[i] = j;
                int sliceSize = 20; // prob should be configurable
                var tmp = res.Skip(i * 20).Take(sliceSize).ToArray(); // tmp = res[i*20 .. slice]
                Array.Copy(res, j * 20, res, i * 20, sliceSize);	  // res[i] = res[j*20 .. slice]
                Array.Copy(tmp, 0, res, j * 20, sliceSize);		      // res[j] = tmp
            }
            order[slices - 1] = slices - 1;

            rkey = key;
            return res.Select<byte, byte>(x => (byte)(x ^ key)).ToArray();
        }
    }
}
