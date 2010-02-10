using System.Collections.ObjectModel;
using SimpleORM.Attributes;


namespace Samples.Entity
{
	[TableMap(new int[] { 0 })]
	public class Node
	{
		[DataColumnMap("Id")]
		public int Id { get; set; }

		[DataColumnMap("Name")]
		public string Name { get; set; }

		[DataRelationMap("NodeNode")]
		[DataRelationColumnMap("Id", "ParentId")]
		public Collection<Node> Children { get; set; }


		public override string ToString()
		{
			return ToString(string.Empty);
		}

		protected string ToString(string tab)
		{
			string r = tab + Id + " - " + Name;
			foreach (var item in Children)
				r += "\n\r" + item.ToString(tab + "\t");

			return r;
		}
	}
}
