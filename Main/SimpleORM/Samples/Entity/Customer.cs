using System;
using System.Collections.Generic;
using System.Text;
using SimpleORM.Attributes;


namespace Samples.Entity
{
	public class Customer
	{
		private int _CustomerId;
		private string _CustomerName;
		private OrderCollection _Orders;

		[DataColumnMap("ID")]
		public int CustomerId
		{
			get { return _CustomerId; }
			set { _CustomerId = value; }
		}

		[DataColumnMap]
		public string CustomerName
		{
			get { return _CustomerName; }
			set { _CustomerName = value; }
		}

		[DataRelationMap("CUSTOMER_TO_ORDER", typeof(Order))]
		public OrderCollection Orders
		{
			get { return _Orders; }
			set { _Orders = value; }
		}


		public override string ToString()
		{
			string r = "Customer: " + CustomerId + " - " + CustomerName + "\n\r";
			foreach (Order item in Orders)
			{
				r += "\t" + item.OrderId + " - " + item.OrderDate + "\n\r";
			}

			return r;
		}
	}
}
