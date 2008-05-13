using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using System.Collections;
using CodeGenerator.Attributes;


namespace CodeGenerator
{
/*	
 * This classes is used in debug purposes
 */
#if DEBUG

	class DebugGeneratorCode
	{
		static void Main(string[] args)
		{
			AppDomain myDomain = Thread.GetDomain();
			AssemblyName myAsmName = new AssemblyName();
			myAsmName.Name = "DataPropSetterAsm";

			AssemblyBuilder myAsmBuilder = myDomain.DefineDynamicAssembly(
				myAsmName, AssemblyBuilderAccess.RunAndSave);

			ModuleBuilder IntVectorModule = myAsmBuilder.DefineDynamicModule("DataPropSetterModule");
			TypeBuilder tb = IntVectorModule.DefineType("PointSetter", TypeAttributes.Class | TypeAttributes.Public);
			MethodBuilder mb = tb.DefineMethod("SetPointProps", MethodAttributes.Public | MethodAttributes.Static,
				CallingConventions.Standard, typeof(void), new Type[] { typeof(MyTest), typeof(DataRow) });
			
			ParameterBuilder pb1 = mb.DefineParameter(1, ParameterAttributes.In, "mt");
			ParameterBuilder pb2 = mb.DefineParameter(2, ParameterAttributes.In, "row");

			ILGenerator ILout = mb.GetILGenerator();

			ILout.Emit(OpCodes.Nop);
			ILout.Emit(OpCodes.Ldarg_0);
			ILout.Emit(OpCodes.Ldarg_1);
			ILout.Emit(OpCodes.Ldstr, "Field1");
			ILout.EmitCall(OpCodes.Call, typeof(DataRow).GetMethod("get_Item", new Type[] { typeof(string)}), null);
			ILout.Emit(OpCodes.Unbox_Any, typeof (Int32));
			ILout.EmitCall(OpCodes.Call, typeof(MyTest).GetMethod("set_Prop1"), null);
			ILout.Emit(OpCodes.Nop);
			ILout.Emit(OpCodes.Ret);
			//ILout.De
			
			myAsmBuilder.Save("myasm.dll");
			tb.CreateType();

			object obj = myAsmBuilder.CreateInstance("PointSetter");

			DataTable dt = new DataTable();
			dt.Columns.Add(new DataColumn("Field1", typeof(int)));
			dt.Columns.Add(new DataColumn("Field2", typeof(string)));
			dt.Columns.Add(new DataColumn("Field3", typeof(DateTime)));
			dt.Rows.Add(72, "Hey", DateTime.Now);
			dt.Rows.Add(DBNull.Value, DBNull.Value, DBNull.Value);

			MyTest mt = new MyTest();

			SetNullable(mt, dt.Rows[0]);
			SetNullableNI(mt, dt.Rows[0]);
			SetValue(mt, dt.Rows[0]);
			SetValueNI(mt, dt.Rows[0]);
			SetRef(mt, dt.Rows[0]);

			SetNullable(mt, dt.Rows[1]);
			SetNullableNI(mt, dt.Rows[1]);
			SetValue(mt, dt.Rows[1]);
			SetValueNI(mt, dt.Rows[1]);
			SetRef(mt, dt.Rows[1]);

			mt.Prop1 = 2;
			mt.GetType().GetMethod("set_Prop1").Invoke(mt, new object[] { 34 });
			myAsmBuilder.GetType("PointSetter").GetMethod("SetPointProps").Invoke(null, new object[] { mt, dt.Rows[0] });
			//mb.GetBaseDefinition().Invoke(obj, new object[] { mt, dt.Rows[0] });
		}

		public static void GenerateMethod(TypeBuilder tb, Type targetClass)
		{
			MethodBuilder mb = tb.DefineMethod("SetPointProps", MethodAttributes.Public | MethodAttributes.Static,
				CallingConventions.Standard, typeof(void), new Type[] { targetClass, typeof(DataRow) });

			mb.DefineParameter(1, ParameterAttributes.In, "target");
			mb.DefineParameter(2, ParameterAttributes.In, "row");

			ILGenerator ILout = mb.GetILGenerator();
			
			//GenerateSetProperty(ILout, ...);

			ILout.Emit(OpCodes.Nop);
			ILout.Emit(OpCodes.Ret);
		}

		public static void GenerateSetProperty(ILGenerator ILout, int column, MethodInfo getItem, MethodInfo setProp, Type propType)
		{
			ILout.Emit(OpCodes.Nop);
			ILout.Emit(OpCodes.Ldarg_0);
			ILout.Emit(OpCodes.Ldarg_1);
			ILout.Emit(OpCodes.Ldc_I4, column);
			ILout.EmitCall(OpCodes.Call, getItem, null);
			//ILout.EmitCall(OpCodes.Call, typeof(DataRow).GetMethod("get_Item", new Type[] { typeof(string) }), null);
			ILout.Emit(propType.IsValueType ? OpCodes.Unbox_Any : OpCodes.Castclass, propType);
			ILout.EmitCall(OpCodes.Call, setProp, null);
			//ILout.EmitCall(OpCodes.Call, typeof(MyTest).GetMethod("set_Prop1"), null);
			//ILout.Emit(OpCodes.Nop);
			//ILout.Emit(OpCodes.Ret);
		}



		public static void SetNullable(MyTest mt, DataRow dr)
		{
			object val = dr[2];

			if (val == DBNull.Value)
				mt.Date = null;
			else
				mt.Date = (DateTime)val;
		}

		public static void SetNullableNI(MyTest mt, DataRow dr)
		{
			object val = dr[0];
			if (val == DBNull.Value)
				mt.BoolProp = null;
			else
				mt.BoolProp = (bool)Convert.ChangeType(val, typeof(bool));
		}

		public static void SetValue(MyTest mt, DataRow dr)
		{
			object val = dr[0];
			if (val == DBNull.Value)
				mt.Prop1 = default(int);
			else
				mt.Prop1 = (int)val;
		}

		public static void SetValueNI(MyTest mt, DataRow dr)
		{
			object val = dr[0];
			if (val == DBNull.Value)
				mt.BoolProp2 = default(bool);
			else
				mt.BoolProp2 = (bool)Convert.ChangeType(val, typeof(bool));
		}

		public static void SetRef(MyTest mt, DataRow dr)
		{
			object val = dr[1];
			if (val == DBNull.Value)
				mt.Prop2 = null;
			else
				mt.Prop2 = (string)val;
		}

		public static void SetStruct(MyTest mt, DataRow dr)
		{
			object val = dr[1];
			if (val == DBNull.Value)
				mt.StructProp = default(DateTime);
			else
				mt.StructProp = (DateTime)val;
		}

		public static void SetNested(MyTest mt, DataRow dr)
		{
			DataRow[] drChilds = dr.GetChildRows("RelationName");
			if (drChilds.Length <= 0)
				return;

			MyCollection newList;
			if (mt.NestedList == null)
			{
				newList = (MyCollection)Activator.CreateInstance(typeof(MyCollection));
				mt.NestedList = newList;
			}
			else
				newList = mt.NestedList;

			//if (newList is List<MyTest>)
			//   newList.Capacity = drChilds.Length + newList.Count;

			ExtractNested(mt.NestedList, typeof(MyTest), drChilds, 234);
		}

		public static void Test(object o)
		{
			if (o is List<MyTest>)
				((List<MyTest>)o).Capacity = 23;
		}

		public static void ExtractNested(IList objectList, Type objectType, IEnumerable<DataRow> dataRows, int schemeId)
		{
		}
	}

	public class MyTest
	{
		private bool _BoolProp2;
		public bool BoolProp2
		{
			get { return _BoolProp2; }
			set { _BoolProp2 = value; }
		}

		private bool? _BoolProp;
		public bool? BoolProp
		{
			get { return _BoolProp; }
			set { _BoolProp = value; }
		}

		private DateTime? _Date;
		public DateTime? Date
		{
			get { return _Date; }
			set { _Date = value; }
		}

		private int _Prop1;
		[DataColumnMap("Field1")]
		public int Prop1
		{
			get { return _Prop1; }
			set { _Prop1 = value; }
		}

		private string _Prop2;
		[DataColumnMapAttribute("Field2")]
		public string Prop2
		{
			get { return _Prop2; }
			set { _Prop2 = value; }
		}

		private DateTime _StructProp;
		public DateTime StructProp 
		{ 
			get { return _StructProp; }
			set { _StructProp = value; }
		}

		private MyCollection _NestedList;
		[DataRelationMap("RelationName")]
		public MyCollection NestedList
		{
			get { return _NestedList; }
			set { _NestedList = value; }
		}
	}

	public class MyCollection : List<MyTest>
	{ }

#endif
}
