﻿namespace Ecng.Test.Data
{
	using System.Data.SqlClient;
	using System.Linq;

	using Ecng.Data;
	using Ecng.Serialization;
	using Ecng.UnitTesting;

	using Microsoft.VisualStudio.TestTools.UnitTesting;

	[TestClass]
	public class DataTests
	{
		[TestMethod]
		public void AddRemove()
		{
			var cache = new DatabaseConnectionCache();
			var created = 0;
			var deleted = 0;
			cache.ConnectionCreated += p => created++;
			cache.ConnectionDeleted += p => deleted++;
			var pair = cache.GetConnection(typeof(SqlConnection), "123");
			cache.Connections.Count().AssertEqual(1);
			created.AssertEqual(1);
			deleted.AssertEqual(0);
			cache.DeleteConnection(pair);
			cache.Connections.Count().AssertEqual(0);
			created.AssertEqual(1);
			deleted.AssertEqual(1);
		}

		[TestMethod]
		public void SaveLoad()
		{
			var cache = new DatabaseConnectionCache();
			var pair = cache.GetConnection(typeof(SqlConnection), "123");
			var ser = new JsonSerializer<DatabaseConnectionCache> { FillMode = true };
			var cache2 = ser.Deserialize(ser.Serialize(cache));

			cache2.Connections.Count().AssertEqual(cache.Connections.Count());
			var pair2 = cache2.Connections.First();
			pair2.Provider.AssertSame(pair.Provider);
			pair2.ConnectionString.AssertEqual(pair.ConnectionString);
		}
	}
}
