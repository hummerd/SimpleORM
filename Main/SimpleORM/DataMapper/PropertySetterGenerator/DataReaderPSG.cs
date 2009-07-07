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
		protected static MethodInfo _GetType = typeof(Type).GetMethod("GetTypeFromHandle");
		protected static MethodInfo _ChangeType = typeof(Convert).GetMethod("ChangeType", new Type[] { typeof(object), typeof(Type) });
		
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


		public void GenerateSetterMethod(ILGenerator ilGen, Type targetClassType, int schemeId, DataTable schemaTable, GetPropertyMapping getPropertyMapping)
		{
			PropertyInfo[] props = targetClassType.GetProperties();

			/* Setter method algorithm
			 * 
			 *if (dr.IsDBNull(0))
		    *		obj.Prop1 = default(int);
			 *else
			 *		obj.Prop1 = dr.GetInt32(0);
			 */


			foreach (PropertyInfo prop in props)
			{
				DataMapAttribute mapping = getPropertyMapping(prop, schemeId);
				if (mapping == null)
					continue;

				if (mapping is DataColumnMapAttribute)
					CreateExtractScalar(targetClassType, prop, ilGen, mapping as DataColumnMapAttribute, schemaTable);
				//else
				//   CreateExtractNested(targetClassType, prop, ilGen, mapping as DataRelationMapAttribute);
			}

			ilGen.Emit(OpCodes.Ret);
		}


		protected void CreateExtractScalar(Type targetClassType, PropertyInfo prop, ILGenerator ilGen, DataColumnMapAttribute mapping, DataTable schemaTable)
		{
			int column = schemaTable.Columns.IndexOf(mapping.MappingName);
			if (column < 0)
				return;

			MethodInfo targetProp = targetClassType.GetMethod("set_" + prop.Name);

			Label lblElse = ilGen.DefineLabel();
			Label lblEnd = ilGen.DefineLabel();

			GenerateMethodHeader(ilGen, column);

			ilGen.Emit(OpCodes.Brfalse, lblElse);

			Type dbType = schemaTable.Columns[column].DataType;
			SetterType setterType = GetSetterType(prop, dbType);
			CreateSetNullValue(setterType, ilGen, prop.PropertyType, targetProp);

			ilGen.Emit(OpCodes.Br, lblEnd);
			ilGen.MarkLabel(lblElse);

			bool readerMethodExist = _ReaderGetMethods.ContainsKey(dbType);
			bool useDirectSet = readerMethodExist && prop.PropertyType == dbType;

			if (setterType == SetterType.Nullable && prop.PropertyType.GetGenericArguments()[0] == dbType)
				GenerateSetDirect(ilGen, column, targetProp, prop.PropertyType, _ReaderGetMethods[dbType], dbType);
			else if (setterType == SetterType.NullableNI)
				CreateSetNotNullValueFromSubType(ilGen, column, targetProp, prop.PropertyType, prop.PropertyType.GetGenericArguments()[0]);
			else if (useDirectSet)
				GenerateSetDirect(ilGen, column, targetProp, prop.PropertyType, _ReaderGetMethods[dbType], null);
			else
				CreateSetNotNullValue(ilGen, column, targetProp, prop.PropertyType);
			
			ilGen.MarkLabel(lblEnd);
		}

		protected void GenerateMethodHeader(ILGenerator ilOut, int column)
		{
			ilOut.DeclareLocal(typeof(object));

			ilOut.Emit(OpCodes.Ldarg_1);
			ilOut.Emit(OpCodes.Ldc_I4, column);
			ilOut.EmitCall(OpCodes.Call, _IsDBNull, null);
		}

		protected void GenerateSetDirect(ILGenerator ilOut, int column, MethodInfo setProp, Type propType, MethodInfo readerGetMethod, Type subType)
		{
			ilOut.Emit(OpCodes.Ldarg_0);
			ilOut.Emit(OpCodes.Ldarg_1);
			ilOut.Emit(OpCodes.Ldc_I4, column);
			ilOut.EmitCall(OpCodes.Callvirt, readerGetMethod, null);

			if (subType != null)
				ilOut.Emit(OpCodes.Newobj, propType.GetConstructor(new Type[] { subType }));

			ilOut.EmitCall(OpCodes.Callvirt, setProp, null);
		}

		protected void CreateSetNotNullValue(ILGenerator ilOut, int column, MethodInfo setProp, Type propType)
		{
			ilOut.Emit(OpCodes.Ldarg_1);
			ilOut.Emit(OpCodes.Ldc_I4, column);
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

		protected void CreateSetNotNullValueFromSubType(ILGenerator ilOut, int column, MethodInfo setProp, Type propType, Type subType)
		{
			ilOut.Emit(OpCodes.Ldarg_1);
			ilOut.Emit(OpCodes.Ldc_I4, column);
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
