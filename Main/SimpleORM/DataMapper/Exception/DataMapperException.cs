using System;


namespace SimpleORM.Exception
{
	public class DataMapperException : System.Exception
	{
		public DataMapperException()
		{

		}

		public DataMapperException(string message)
			: base (message)
		{

		}
	}
}
