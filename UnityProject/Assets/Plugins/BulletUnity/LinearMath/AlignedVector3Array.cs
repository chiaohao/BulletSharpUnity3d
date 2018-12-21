using BulletSharp.Math;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using static BulletSharp.UnsafeNativeMethods;

namespace BulletSharp
{
	[DebuggerDisplay("Count = {Count}")]
	[DebuggerTypeProxy(typeof(Vector3ListDebugView))]
	public class AlignedVector3Array : BulletObject, IList<Vector3>
	{
        public AlignedVector3Array(IntPtr native)
		{
			Initialize(native);
		}

        public AlignedVector3Array()
        {
            Initialize(btAlignedObjectArray_btVector3_new());
        }

		public int IndexOf(Vector3 item)
		{
			throw new NotImplementedException();
		}

		public void Insert(int index, Vector3 item)
		{
			throw new NotImplementedException();
		}

		public void RemoveAt(int index)
		{
			throw new NotImplementedException();
		}

		public Vector3 this[int index]
		{
			get
			{
				if ((uint)index >= (uint)Count)
				{
					throw new ArgumentOutOfRangeException(nameof(index));
				}
				Vector3 value;
				btAlignedObjectArray_btVector3_at(Native, index, out value);
				return value;
			}
			set
			{
				if ((uint)index >= (uint)Count)
				{
					throw new ArgumentOutOfRangeException(nameof(index));
				}
				btAlignedObjectArray_btVector3_set(Native, index, ref value);
			}
		}

        public ICollection<Vector3> GetVector3s()
        {
            ICollection<Vector3> vector3s = new List<Vector3>();
            int size = btAlignedObjectArray_btVector3_size(Native);
            for(int i = 0; i < size; i++)
            {
                Vector3 vector3 = new Vector3();
                btAlignedObjectArray_btVector3_at(Native, i, out vector3);
                vector3s.Add(vector3);
            }
                
            return vector3s;
        }


        public void Add(Vector3 item)
		{
			btAlignedObjectArray_btVector3_push_back(Native, ref item);
		}

        public void Add(Vector4 item)
        {
            btAlignedObjectArray_btVector3_push_back2(Native, ref item);
        }

        public void Clear()
		{
			throw new NotImplementedException();
		}

		public bool Contains(Vector3 item)
		{
			throw new NotImplementedException();
		}

		public void CopyTo(Vector3[] array, int arrayIndex)
		{
			if (array == null)
				throw new ArgumentNullException(nameof(array));

			if (arrayIndex < 0)
				throw new ArgumentOutOfRangeException(nameof(array));

			int count = Count;
			if (arrayIndex + count > array.Length)
				throw new ArgumentException("Array too small.", "array");

			for (int i = 0; i < count; i++)
			{
				array[arrayIndex + i] = this[i];
			}
		}

		public int Count => btAlignedObjectArray_btVector3_size(Native);

		public bool IsReadOnly => false;

		public bool Remove(Vector3 item)
		{
			throw new NotImplementedException();
		}

		public IEnumerator<Vector3> GetEnumerator()
		{
			return new Vector3ArrayEnumerator(this);
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return new Vector3ArrayEnumerator(this);
		}
	}
}
