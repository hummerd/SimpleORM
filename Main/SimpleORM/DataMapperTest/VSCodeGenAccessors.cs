﻿// ------------------------------------------------------------------------------
//<autogenerated>
//        This code was generated by Microsoft Visual Studio Team System 2005.
//
//        Changes to this file may cause incorrect behavior and will be lost if
//        the code is regenerated.
//</autogenerated>
//------------------------------------------------------------------------------
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SimpleORM.Attributes;

namespace DataMapperTest
{
[System.Diagnostics.DebuggerStepThrough()]
[System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.TestTools.UnitTestGeneration", "1.0.0.0")]
internal class BaseAccessor {
    
    protected Microsoft.VisualStudio.TestTools.UnitTesting.PrivateObject m_privateObject;
    
    protected BaseAccessor(object target, Microsoft.VisualStudio.TestTools.UnitTesting.PrivateType type) {
        m_privateObject = new Microsoft.VisualStudio.TestTools.UnitTesting.PrivateObject(target, type);
    }
    
    protected BaseAccessor(Microsoft.VisualStudio.TestTools.UnitTesting.PrivateType type) : 
            this(null, type) {
    }
    
    internal virtual object Target {
        get {
            return m_privateObject.Target;
        }
    }
    
    public override string ToString() {
        return this.Target.ToString();
    }
    
    public override bool Equals(object obj) {
        if (typeof(BaseAccessor).IsInstanceOfType(obj)) {
            obj = ((BaseAccessor)(obj)).Target;
        }
        return this.Target.Equals(obj);
    }
    
    public override int GetHashCode() {
        return this.Target.GetHashCode();
    }
}


[System.Diagnostics.DebuggerStepThrough()]
[System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.TestTools.UnitTestGeneration", "1.0.0.0")]
internal class CodeGenerator_DataMapperAccessor : BaseAccessor {

	protected static Microsoft.VisualStudio.TestTools.UnitTesting.PrivateType m_privateType = new Microsoft.VisualStudio.TestTools.UnitTesting.PrivateType(typeof(global::SimpleORM.DataMapper));
    
    internal CodeGenerator_DataMapperAccessor() : 
            base(m_privateType) {
    }
    
    internal static global::System.Xml.XmlDocument _XmlDocument {
        get {
            global::System.Xml.XmlDocument ret = ((global::System.Xml.XmlDocument)(m_privateType.GetStaticField("_XmlDocument")));
            return ret;
        }
        set {
            m_privateType.SetStaticField("_XmlDocument", value);
        }
    }
    
    internal static global::System.Reflection.Emit.TypeBuilder _TypeBulider {
        get {
            global::System.Reflection.Emit.TypeBuilder ret = ((global::System.Reflection.Emit.TypeBuilder)(m_privateType.GetStaticField("_TypeBulider")));
            return ret;
        }
        set {
            m_privateType.SetStaticField("_TypeBulider", value);
        }
    }
    
    internal static global::System.Reflection.MethodInfo GenerateMethod(global::System.Type objectType, int schemeId, global::System.Data.DataTable dtSource) {
        object[] args = new object[] {
                objectType,
                schemeId,
                dtSource};
        global::System.Reflection.MethodInfo ret = ((global::System.Reflection.MethodInfo)(m_privateType.InvokeStatic("GenerateMethod", new System.Type[] {
                    typeof(global::System.Type),
                    typeof(int),
                    typeof(global::System.Data.DataTable)}, args)));
        return ret;
    }
    
    internal static global::System.Reflection.MethodInfo GenerateMethodFromType(global::System.Type targetClass, int schemeId, global::System.Data.DataTable dtSource) {
        object[] args = new object[] {
                targetClass,
                schemeId,
                dtSource};
        global::System.Reflection.MethodInfo ret = ((global::System.Reflection.MethodInfo)(m_privateType.InvokeStatic("GenerateMethodFromType", new System.Type[] {
                    typeof(global::System.Type),
                    typeof(int),
                    typeof(global::System.Data.DataTable)}, args)));
        return ret;
    }
    
    internal static global::System.Reflection.MethodInfo GenerateMethodFromXml(global::System.Type targetClass, int schemeId, global::System.Data.DataTable dtSource) {
        object[] args = new object[] {
                targetClass,
                schemeId,
                dtSource};
        global::System.Reflection.MethodInfo ret = ((global::System.Reflection.MethodInfo)(m_privateType.InvokeStatic("GenerateMethodFromXml", new System.Type[] {
                    typeof(global::System.Type),
                    typeof(int),
                    typeof(global::System.Data.DataTable)}, args)));
        return ret;
    }
    
    internal static void CreateDynamicAssembly() {
        object[] args = new object[0];
        m_privateType.InvokeStatic("CreateDynamicAssembly", new System.Type[0], args);
    }
    
    internal static object ExtractComplexObject(global::System.Data.DataRow dr, global::System.Type objectType, bool allowNullValues, int schemeId, bool extractRelated) {
        object[] args = new object[] {
                dr,
                objectType,
                allowNullValues,
                schemeId,
                extractRelated};
        object ret = ((object)(m_privateType.InvokeStatic("ExtractComplexObject", new System.Type[] {
                    typeof(global::System.Data.DataRow),
                    typeof(global::System.Type),
                    typeof(bool),
                    typeof(int),
                    typeof(bool)}, args)));
        return ret;
    }

	 internal static global::SimpleORM.Attributes.DataMapAttribute FindAttribute(int schemeId, object[] attrs)
	 {
        object[] args = new object[] {
                schemeId,
                attrs};
		  global::SimpleORM.Attributes.DataMapAttribute ret = ((global::SimpleORM.Attributes.DataMapAttribute)(m_privateType.InvokeStatic("FindAttribute", new System.Type[] {
                    typeof(int),
                    typeof(object).MakeArrayType()}, args)));
        return ret;
    }

	 internal static bool ExtractValue(global::SimpleORM.Attributes.DataColumnMapAttribute columnMapAttr, object obj, global::System.Data.DataRow dr, global::System.Reflection.PropertyInfo prop, bool allowNullValues)
	 {
        object[] args = new object[] {
                columnMapAttr,
                obj,
                dr,
                prop,
                allowNullValues};
        bool ret = ((bool)(m_privateType.InvokeStatic("ExtractValue", new System.Type[] {
                    typeof(global::SimpleORM.Attributes.DataColumnMapAttribute),
                    typeof(object),
                    typeof(global::System.Data.DataRow),
                    typeof(global::System.Reflection.PropertyInfo),
                    typeof(bool)}, args)));
        return ret;
    }

	 internal static void ExtractNestedObjects(global::SimpleORM.Attributes.DataRelationMapAttribute relationMapAttr, object obj, global::System.Data.DataRow dr, global::System.Reflection.PropertyInfo prop, bool allowNullValues)
	 {
        object[] args = new object[] {
                relationMapAttr,
                obj,
                dr,
                prop,
                allowNullValues};
        m_privateType.InvokeStatic("ExtractNestedObjects", new System.Type[] {
                    typeof(global::SimpleORM.Attributes.DataRelationMapAttribute),
                    typeof(object),
                    typeof(global::System.Data.DataRow),
                    typeof(global::System.Reflection.PropertyInfo),
                    typeof(bool)}, args);
    }
}
}
