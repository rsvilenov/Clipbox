using System;
using System.Runtime.InteropServices;

namespace Clipbox.Infrastructure.CLR
{
    public static class ClrHelpers
    {
        /// <summary>
        /// Tries to get the size in memory of a managed object. This is, mildly put, experimental, so 
        /// it is not implemented as an extention method of 'object' in an attempt to limit its usage.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static long GetManagedSize(object obj)
        {
            // try to get the size of the object, relying on the poorly documented hack, found here:
            // https://stackoverflow.com/questions/1128315/find-size-of-object-instance-in-bytes-in-c-sharp
            // https://www.codeproject.com/Articles/1254217/Calculating-Heap-Size-of-Managed-Objects
            byte[] dwFlags1 = new byte[2];
            dwFlags1[0] = Marshal.ReadByte(obj.GetType().TypeHandle.Value);
            dwFlags1[1] = Marshal.ReadByte(obj.GetType().TypeHandle.Value, 1);
            short componentSize = BitConverter.ToInt16(dwFlags1, 0);
            int baseSize = Marshal.ReadInt32(obj.GetType().TypeHandle.Value, 4);

            if (obj.GetType().IsArray)
            {
                return -1;
                //return baseSize + (obj as Array).Length * componentSize;
            }
            else if (obj is string)
            {
                return baseSize + (obj as string).Length * componentSize;
            }

            return baseSize;
            
                //unsafe
                //{
                //    RuntimeTypeHandle th = obj.GetType().TypeHandle;
                //    return *(*(int**)&th + 1);
                //}
            }
    }
}
