using System;
using System.Collections.Generic;
using System.Text;
using SimpleORM.Attributes;


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

		[DataRelationMap("ParentChild1")]
		[DataRelationColumnMap("Id", "ParentId")]
		public Child1Collection Childs1 { get; set; }

		[DataRelationMap("ParentChild2")]
		[DataRelationColumnMap("Id", "ParentId")]
		public List<Child2> Childs2 { get; set; }
	}

	[DataRelatedToMap(typeof(Parent))]
	public class Child1 : Entity
	{
		public DateTime? Date { get; set; }
		public EntityType EntityType { get; set; }
	}

	public class Child1Collection : List<Child1>
	{ }

	[DataRelatedToMap(typeof(Parent))]
	public class Child2 : Entity
	{
		public DateTime? Date { get; set; }
		public EntityType EntityType { get; set; }

		[DataRelationMap("Child2Child")]
		[DataRelationColumnMap("Id", "ParentId")]
		public List<Child2Child> Childs2 { get; set; }
	}

	[DataRelatedToMap(typeof(Child2))]
	public class Child2Child : Entity
	{
		public DateTime? Date { get; set; }
		public EntityType EntityType { get; set; }
	}
}
