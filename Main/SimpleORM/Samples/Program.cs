using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using SimpleORM;
using SimpleORM.Attributes;
using Samples.Entity;


namespace Samples
{
	class Program
	{
		private static DataMapper attMapper = new DataMapper();
		private static DataMapper xmlMapper = new DataMapper(
			new List<string> { @"..\..\customer.mapping", @"..\..\node.mapping" });


		static void Main(string[] args)
		{
			DataSetToCustomersWithOrders();
			DataReaderToCustomersWithOrders();
			DataReaderToTree();
			DataTableToTree();

			Console.Read();
		}


		private static void DataSetToCustomersWithOrders()
		{
			Console.WriteLine("DataSetToCustomersWithOrders");

			DataSet dataSet = LoadCustomerDataSet();
			//when working with dataset you must create parent\child relations manually
			dataSet.Relations.Add("CUSTOMER_TO_ORDER",
				dataSet.Tables["Customers"].Columns["ID"],
				dataSet.Tables["Orders"].Columns["ParentID"],
				false);

			List<Customer> customers = new List<Customer>();
			attMapper.FillObjectList<Customer>(dataSet.Tables["Customers"].Rows, customers);
			foreach (var item in customers)
			{
				Console.WriteLine(item);
			}
		}

		private static void DataReaderToCustomersWithOrders()
		{
			Console.WriteLine("DataReaderToCustomersWithOrders");

			IDataReader reader = LoadCustomerDataReader();

			List<Customer> customers = new List<Customer>();
			//note: Some overloads of this method closes reader and some do not
			xmlMapper.FillObjectListComplex<Customer>(reader, customers);
			reader.Close();

			foreach (var item in customers)
			{
				Console.WriteLine(item);
			}
		}

		private static void DataTableToTree()
		{
			Console.WriteLine("DataTableToTree");
			DataTable dtNodes = LoadNodeDataTable();

			DataSet parent = new DataSet();
			parent.Tables.Add(dtNodes);
			parent.Relations.Add(new DataRelation(
				"NodeNode",
				dtNodes.Columns["Id"],
				dtNodes.Columns["ParentId"]
				));

			//Filter data table to have only top level nodes in tree collection
			DataView dvParents = new DataView(dtNodes);
			dvParents.RowFilter = "ParentId is null";

			List<Node> tree = new List<Node>();
			attMapper.FillObjectList<Node>(dvParents, tree);
			Console.WriteLine(tree[0].ToString());
		}

		private static void DataReaderToTree()
		{
			Console.WriteLine("DataReaderToTree");
			IDataReader drNodes = LoadNodeDataReader();

			List<Node> tree = new List<Node>();
			xmlMapper.FillObjectListComplex<Node>(drNodes, tree, 0, null, true,
				//This filter allow only top level nodes appear in tree collection
				(r, n) => r.IsDBNull(2));
			Console.WriteLine(tree[0].ToString());
		}


		private static IDataReader LoadCustomerDataReader()
		{
			return LoadCustomerDataSet().CreateDataReader();
		}

		private static DataSet LoadCustomerDataSet()
		{
			DataSet result = new DataSet();

			DataTable dtCustomers = result.Tables.Add("Customers");
			dtCustomers.Columns.Add(new DataColumn("ID", typeof(int)));
			dtCustomers.Columns.Add(new DataColumn("CustomerName", typeof(string)));

			dtCustomers.Rows.Add(72, "Pete");
			dtCustomers.Rows.Add(34, "John");
			dtCustomers.Rows.Add(21, "Mike");
			dtCustomers.Rows.Add(DBNull.Value, DBNull.Value);

			DataTable dtOrders = result.Tables.Add("Orders");
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

			return result;
		}

		private static IDataReader LoadNodeDataReader()
		{
			return LoadNodeDataTable().CreateDataReader();
		}

		private static DataTable LoadNodeDataTable()
		{
			DataTable dtNode = new DataTable("Node");
			dtNode.Columns.Add(new DataColumn("Id", typeof(int)));
			dtNode.Columns.Add(new DataColumn("Name", typeof(string)));
			dtNode.Columns.Add(new DataColumn("ParentId", typeof(int)));

			dtNode.Rows.Add(1, "Node1", DBNull.Value);
			dtNode.Rows.Add(2, "Node2", 1);
			dtNode.Rows.Add(3, "Node3", 1);
			dtNode.Rows.Add(4, "Node3", 3);
			dtNode.Rows.Add(5, "Node3", 3);

			return dtNode;
		}
	}
}
