using System;
using System.Collections.Generic;
using System.Text;


namespace DataMapperTest
{
	public class Entity
	{
		public int Id { get; set; }
		public string Name { get; set; }
	}

	public class EntityType
	{
		public int Id { get; set; }
		public string Name { get; set; }
	}

	public class Parent : Entity
	{
		public DateTime? Date{ get; set; }
		public EntityType EntityType { get; set; }
		public Child1Collection Childs1 { get; set; }
		public List<Child2> Childs2 { get; set; }
	}

	public class Child1 : Entity
	{
		public DateTime? Date { get; set; }
		public EntityType EntityType { get; set; }
	}

	public class Child1Collection : List<Child1>
	{ }

	public class Child2 : Entity
	{
		public DateTime? Date { get; set; }
		public EntityType EntityType { get; set; }
		public List<Child2Child> Childs2 { get; set; }
	}

	public class Child2Child : Entity
	{
		public DateTime? Date { get; set; }
		public EntityType EntityType { get; set; }
	}
}
