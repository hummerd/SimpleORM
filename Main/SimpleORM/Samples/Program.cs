using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using SimpleORM;
using SimpleORM.Attributes;


namespace Samples
{
	class Program
	{
		static void Main(string[] args)
		{
			DataSet dsTest = new DataSet();

			DataTable dtCustomers = dsTest.Tables.Add("Customers");
			dtCustomers.Columns.Add(new DataColumn("ID", typeof(int)));
			dtCustomers.Columns.Add(new DataColumn("CustomerName", typeof(string)));

			dtCustomers.Rows.Add(72, "Pete");
			dtCustomers.Rows.Add(34, "John");
			dtCustomers.Rows.Add(21, "Mike");
			dtCustomers.Rows.Add(DBNull.Value, DBNull.Value);

			DataTable dtOrders = dsTest.Tables.Add("Orders");
			dtOrders.Columns.Add(new DataColumn("ID", typeof(int)));
			dtOrders.Columns.Add(new DataColumn("OrderDate", typeof(DateTime)));
			dtOrders.Columns.Add(new DataColumn("ParentID", typeof(int)));

			dtOrders.Rows.Add(1, DateTime.Now, 72);
			dtOrders.Rows.Add(2, DBNull.Value, 72);
			dtOrders.Rows.Add(3, DateTime.Now, 34);
			dtOrders.Rows.Add(4, DBNull.Value, 34);
			dtOrders.Rows.Add(5, DBNull.Value, 34);
			dtOrders.Rows.Add(6, DateTime.Now, 34);
			dtOrders.Rows.Add(DBNull.Value, DBNull.Value, DBNull.Value);

			dsTest.Relations.Add("CUSTOMER_TO_ORDER",
				dtCustomers.Columns["ID"],
				dtOrders.Columns["ParentID"],
				false);

			List<Customer> customers = new List<Customer>();
			DataMapper.Default.FillObjectList<Customer>(customers, dtCustomers.Rows);
		}
	}

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
	}

	public class OrderCollection : ArrayList
	{ }

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
