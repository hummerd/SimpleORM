using System;


namespace SimpleORM
{
	public interface IObjectBuilder
	{
		object CreateObject(Type objectType);
		T CreateObject<T>() where T : new();
	}
}
