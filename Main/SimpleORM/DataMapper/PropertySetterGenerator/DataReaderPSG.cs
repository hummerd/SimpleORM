using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using SimpleORM.Attributes;
using System.Reflection.Emit;
using System.Data;


namespace SimpleORM.PropertySetterGenerator
{
	public class DataReaderPSG : PSGBase, IPropertySetterGenerator
	{
		protected static Dictionary<Type, MethodInfo> _ReaderGetMethods = new Dictionary<Type,MethodInfo>();
		protected static MethodInfo _GetValue = typeof(IDataRecord).GetMethod("GetValue");
		protected static MethodInfo _IsDBNull = typeof(IDataRecord).GetMethod("IsDBNull");
		
		
		//protected static MethodInfo _SetNested = typeof(DataMapper).GetMethod("FillObjectListNested");


		static DataReaderPSG()
		{
			Type typeReader = typeof(IDataRecord);

			_ReaderGetMethods.Add(typeof(int), typeReader.GetMethod("GetInt32", new Type[] { typeof(int) }));
			_ReaderGetMethods.Add(typeof(long), typeReader.GetMethod("GetInt64", new Type[] { typeof(int) }));
			_ReaderGetMethods.Add(typeof(string), typeReader.GetMethod("GetString", new Type[] { typeof(int) }));
			_ReaderGetMethods.Add(typeof(bool), typeReader.GetMethod("GetBoolean", new Type[] { typeof(int) }));
			_ReaderGetMethods.Add(typeof(byte), typeReader.GetMethod("GetByte", new Type[] { typeof(int) }));
			_ReaderGetMethods.Add(typeof(DateTime), typeReader.GetMethod("GetDateTime", new Type[] { typeof(int) }));
			_ReaderGetMethods.Add(typeof(double), typeReader.GetMethod("GetDouble", new Type[] { typeof(int) }));
		}


		public void GenerateSetterMethod(
			ILGenerator ilOut, 
			Type targetClassType, 
			int schemeId, 
			DataTable schemaTable, 
			GetPropertyMapping getPropertyMapping,
			ExtractInfo extractInfo)
		{
			GenerateSetterMethod(
				ilOut, 
				targetClassType, 
				schemeId, 
				schemaTable, 
				getPropertyMapping,
				extractInfo,
				false);
		}


		protected override void CreateExtractScalar(ILGenerator ilOut, Type targetClassType, PropertyInfo prop, DataColumnMapAttribute mapping, DataTable schemaTable, int propIndex)
		{
			int column = schemaTable.Columns.IndexOf(mapping.MappingName);
			if (column < 0)
				return;

			MethodInfo targetProp = targetClassType.GetMethod("set_" + prop.Name);

			Label lblElse = ilOut.DefineLabel();
			Label lblSetNull = ilOut.DefineLabel();
			Label lblEnd = ilOut.DefineLabel();

			GeneratePropSetterHeader(ilOut, propIndex);

			//ilOut.Emit(OpCodes.Brfalse, lblElse);
			ilOut.Emit(OpCodes.Ldc_I4_0);
			ilOut.Emit(OpCodes.Blt, lblSetNull);

			ilOut.Emit(OpCodes.Ldarg_1);
			ilOut.Emit(OpCodes.Ldloc_1);
			ilOut.EmitCall(OpCodes.Call, _IsDBNull, null);
			ilOut.Emit(OpCodes.Brfalse, lblElse);

			ilOut.MarkLabel(lblSetNull);

			Type dbType = schemaTable.Columns[column].DataType;
			SetterType setterType = GetSetterType(prop, dbType);
			CreateSetNullValue(setterType, ilOut, prop.PropertyType, targetProp);

			ilOut.Emit(OpCodes.Br, lblEnd);
			ilOut.MarkLabel(lblElse);

			bool readerMethodExist = _ReaderGetMethods.ContainsKey(dbType);
			bool useDirectSet = readerMethodExist && prop.PropertyType == dbType;

			if (setterType == SetterType.Nullable && prop.PropertyType.GetGenericArguments()[0] == dbType)
				GenerateSetDirect(ilOut, propIndex, targetProp, prop.PropertyType, _ReaderGetMethods[dbType], dbType);
			else if (setterType == SetterType.NullableNI)
				CreateSetNotNullValueFromSubType(ilOut, propIndex, targetProp, prop.PropertyType, prop.PropertyType.GetGenericArguments()[0]);
			else if (useDirectSet)
				GenerateSetDirect(ilOut, propIndex, targetProp, prop.PropertyType, _ReaderGetMethods[dbType], null);
			else
				CreateSetNotNullValue(ilOut, propIndex, targetProp, prop.PropertyType);
			
			ilOut.MarkLabel(lblEnd);
		}

		protected override void CreateExtractNested(ILGenerator ilOut, Type targetClassType, PropertyInfo prop, DataRelationMapAttribute mapping)
		{
			; 
		}


		protected void GeneratePropSetterHeader(ILGenerator ilOut, int propIndex)
		{
			/*
			 *  (0 TargetType obj, 1 DataReader reader, 2 DataMapper mapper, 3 List<List<int>> clmns, 4 ref clmnIx)
			 *  int ix = list[columnsIx]['propIndex'];
			 *  if (ix < 0 || reader.ISDBNull(ix))
			 *		val = DBNull.Value;
			 *	 else
			 *		val = dataRow[ix];
			 *		
			 *  if (DBNull.Value 
			 */

			 //L_0000: ldarg.1 
			 //L_0001: ldarg.2 
			 //L_0002: callvirt instance !0 [mscorlib]System.Collections.Generic.List`1<class [mscorlib]System.Collections.Generic.List`1<int32>>::get_Item(int32)
			 //L_0007: ldc.i4.0 
			 //L_0008: callvirt instance !0 [mscorlib]System.Collections.Generic.List`1<int32>::get_Item(int32)
			 //L_000d: stloc.1 
			 //L_000e: ldloc.1 
			 //L_000f: ldc.i4.0 
			 //L_0010: blt.s L_001b
			 //L_0012: ldarg.0 
			 //L_0013: ldloc.1 
			 //L_0014: callvirt instance bool [System.Data]System.Data.IDataRecord::IsDBNull(int32)
			 //L_0019: brfalse.s L_0023
			 //L_001b: ldsfld class [mscorlib]System.DBNull [mscorlib]System.DBNull::Value
			 //L_0020: stloc.0 
			 //L_0021: br.s L_002b
			 //L_0023: ldarg.0 
			 //L_0024: ldloc.1 
			 //L_0025: callvirt instance object [System.Data]System.Data.IDataRecord::get_Item(int32)
			 //L_002a: stloc.0 
			 //L_002b: ldloc.0 
			 //L_002c: callvirt instance string [mscorlib]System.Object::ToString()

//			ilOut.Emit(OpCodes.Ldarg_1);
			
			ilOut.Emit(OpCodes.Ldarg_3);
			ilOut.Emit(OpCodes.Ldarg, 4);
			ilOut.Emit(OpCodes.Ldind_I4);
			ilOut.EmitCall(OpCodes.Call, _GetSubListItem, null);
			ilOut.Emit(OpCodes.Ldc_I4, propIndex);
			ilOut.EmitCall(OpCodes.Call, _GetListItem, null);

			ilOut.Emit(OpCodes.Stloc_1);
			ilOut.Emit(OpCodes.Ldloc_1);
			//ilOut.Emit(OpCodes.Ldc_I4, column);

			//ilOut.EmitCall(OpCodes.Call, _IsDBNull, null);
		}

		protected void GenerateSetDirect(ILGenerator ilOut, int propIndex, MethodInfo setProp, Type propType, MethodInfo readerGetMethod, Type subType)
		{
			ilOut.Emit(OpCodes.Ldarg_0);
			ilOut.Emit(OpCodes.Ldarg_1);

			////ilOut.Emit(OpCodes.Ldarg_3);
			////ilOut.Emit(OpCodes.Ldarg, 4);
			////ilOut.Emit(OpCodes.Ldind_I4);
			////ilOut.EmitCall(OpCodes.Call, _GetSubListItem, null);
			////ilOut.Emit(OpCodes.Ldc_I4, propIndex);
			////ilOut.EmitCall(OpCodes.Call, _GetListItem, null);
			ilOut.Emit(OpCodes.Ldloc_1);
			//ilOut.Emit(OpCodes.Ldc_I4, column);

			ilOut.EmitCall(OpCodes.Callvirt, readerGetMethod, null);

			if (subType != null)
				ilOut.Emit(OpCodes.Newobj, propType.GetConstructor(new Type[] { subType }));

			ilOut.EmitCall(OpCodes.Callvirt, setProp, null);
		}

		protected void CreateSetNotNullValue(ILGenerator ilOut, int propIndex, MethodInfo setProp, Type propType)
		{
			ilOut.Emit(OpCodes.Ldarg_1);

			//ilOut.Emit(OpCodes.Ldarg_3);
			//ilOut.Emit(OpCodes.Ldarg, 4);
			//ilOut.Emit(OpCodes.Ldind_I4);
			//ilOut.EmitCall(OpCodes.Call, _GetSubListItem, null);
			//ilOut.Emit(OpCodes.Ldc_I4, propIndex);
			//ilOut.EmitCall(OpCodes.Call, _GetListItem, null);
			//ilOut.Emit(OpCodes.Ldc_I4, column);
			ilOut.Emit(OpCodes.Ldloc_1);
			ilOut.EmitCall(OpCodes.Callvirt, _GetValue, null);
			ilOut.Emit(OpCodes.Stloc_0);

			ilOut.Emit(OpCodes.Ldarg_0);
			ilOut.Emit(OpCodes.Ldloc_0);
			ilOut.Emit(OpCodes.Ldtoken, propType);
			ilOut.EmitCall(OpCodes.Call, _GetType, null);
			ilOut.EmitCall(OpCodes.Call, _ChangeType, null);
			ilOut.Emit(OpCodes.Castclass, propType);
			ilOut.EmitCall(OpCodes.Callvirt, setProp, null);
		}

		protected void CreateSetNotNullValueFromSubType(ILGenerator ilOut, int propIndex, MethodInfo setProp, Type propType, Type subType)
		{
			ilOut.Emit(OpCodes.Ldarg_1);

			//ilOut.Emit(OpCodes.Ldarg_3);
			//ilOut.Emit(OpCodes.Ldarg, 4);
			//ilOut.Emit(OpCodes.Ldind_I4);
			//ilOut.EmitCall(OpCodes.Call, _GetSubListItem, null);
			//ilOut.Emit(OpCodes.Ldc_I4, propIndex);
			//ilOut.EmitCall(OpCodes.Call, _GetListItem, null);
			//ilOut.Emit(OpCodes.Ldc_I4, column);
			ilOut.Emit(OpCodes.Ldloc_1);
			ilOut.EmitCall(OpCodes.Callvirt, _GetValue, null);
			ilOut.Emit(OpCodes.Stloc_0);

			ilOut.Emit(OpCodes.Ldarg_0);
			ilOut.Emit(OpCodes.Ldloc_0);
			ilOut.Emit(OpCodes.Ldtoken, subType);
			ilOut.EmitCall(OpCodes.Call, _GetType, null);
			ilOut.EmitCall(OpCodes.Call, _ChangeType, null);
			ilOut.Emit(OpCodes.Unbox_Any, subType);
			ilOut.Emit(OpCodes.Newobj, propType.GetConstructor(new Type[] { subType }));
			ilOut.EmitCall(OpCodes.Callvirt, setProp, null);
		}
	}
}
