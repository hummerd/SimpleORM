using System;


namespace SimpleORM
{
	public interface IObjectBuilder
	{
		object CreateObject(Type objectType);
	}
}
