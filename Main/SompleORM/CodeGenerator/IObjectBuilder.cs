using System;


namespace CodeGenerator
{
	public interface IObjectBuilder
	{
		object CreateObject(Type objectType);
	}
}
