using System;
using System.Collections.Generic;
using System.Text;

namespace SimpleORM.PropertySetterGenerator
{
	public class LinkedKeyCache
		: List<List<KeyInfo>>
	{
		public List<KeyInfo> FindLinkedKeys(KeyInfo key)
		{
			for (int i = 0; i < this.Count; i++)
			{
				if (this[i].Contains(key))
					return this[i];
			}

			return null;
		}
	}
}
