using System;
using System.Collections.Generic;
using System.Text;
using SimpleORM.Attributes;

namespace Samples.Entity
{
	public class Order
	{
		private int _OrderId;
		private DateTime? _OrderDate;

		[DataColumnMap("ID")]
		public int OrderId
		{
			get { return _OrderId; }
			set { _OrderId = value; }
		}

		[DataColumnMap]
		public DateTime? OrderDate
		{
			get { return _OrderDate; }
			set { _OrderDate = value; }
		}
	}
}
