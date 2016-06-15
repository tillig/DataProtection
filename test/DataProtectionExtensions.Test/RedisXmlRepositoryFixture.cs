using System;
using System.Linq;
using System.Xml.Linq;
using Microsoft.Extensions.Logging;
using Moq;
using StackExchange.Redis;
using Xunit;

namespace DataProtectionExtensions.Test
{
	public class RedisXmlRepositoryFixture
	{
		[Fact]
		public void Ctor_Connection_NullConnection()
		{
			var logger = Mock.Of<ILogger<RedisXmlRepository>>();
			Assert.Throws<ArgumentNullException>(() => new RedisXmlRepository((IConnectionMultiplexer)null, logger));
		}

		[Fact]
		public void Ctor_Connection_NullLogger()
		{
			var connection = Mock.Of<IConnectionMultiplexer>();
			Assert.Throws<ArgumentNullException>(() => new RedisXmlRepository(connection, null));
		}

		[Fact]
		public void Ctor_String_NullConnectionString()
		{
			var logger = Mock.Of<ILogger<RedisXmlRepository>>();
			Assert.Throws<ArgumentNullException>(() => new RedisXmlRepository((string)null, logger));
		}

		[Fact]
		public void Dispose_CleansUpConnection()
		{
			var context = CreateProvider();
			context.Repository.Dispose();
			var mockProvider = Mock.Get(context.Connection);

			mockProvider.Verify(x => x.Close(true), Times.Once());
			mockProvider.Verify(x => x.Dispose(), Times.Once());
		}

		[Fact]
		public void Dispose_NoMultipleConnectionDisposal()
		{
			var context = CreateProvider();
			context.Repository.Dispose();
			context.Repository.Dispose();
			context.Repository.Dispose();
			var mockProvider = Mock.Get(context.Connection);
			mockProvider.Verify(x => x.Close(true), Times.Once);
			mockProvider.Verify(x => x.Dispose(), Times.Once);
		}

		[Fact]
		public void GetAllElements_EmptyHash()
		{
			var context = CreateProvider();
			var mockProvider = Mock.Get(context.Database);
			mockProvider.Setup(x => x.HashGetAll(RedisXmlRepository.RedisHashKey, CommandFlags.None)).Returns(new HashEntry[0]);
			var elements = context.Repository.GetAllElements();
			Assert.NotNull(elements);
			Assert.Empty(elements);
		}

		[Fact]
		public void GetAllElements_NullHash()
		{
			var context = CreateProvider();
			var mockProvider = Mock.Get(context.Database);
			mockProvider.Setup(x => x.HashGetAll(RedisXmlRepository.RedisHashKey, CommandFlags.None)).Returns((HashEntry[])null);
			var elements = context.Repository.GetAllElements();
			Assert.NotNull(elements);
			Assert.Empty(elements);
		}

		[Fact]
		public void GetAllElements_ReadsValuesFromRedis()
		{
			var context = CreateProvider();
			var config = new HashEntry[]
			{
				new HashEntry("key1", "<root1 />"),
				new HashEntry("key2", "<root2 />"),
			};
			var mockProvider = Mock.Get(context.Database);
			mockProvider.Setup(x => x.HashGetAll(RedisXmlRepository.RedisHashKey, CommandFlags.None)).Returns(config);
			var elements = context.Repository.GetAllElements();
			Assert.Equal(2, elements.Count);
		}

		[Fact]
		public void StoreElement_EmptyFriendlyName()
		{
			var context = CreateProvider();
			var mockDatabase = Mock.Get(context.Database);
			mockDatabase.Setup(
				x => x.HashSet(
					It.Is<RedisKey>(k => k.Equals(RedisXmlRepository.RedisHashKey)),
					It.Is<RedisValue>(v => !v.IsNullOrEmpty),
					It.IsAny<RedisValue>(),
					When.Always,
					CommandFlags.None)).Verifiable();
			context.Repository.StoreElement(XElement.Parse("<root />"), "");
			mockDatabase.Verify();
		}

		[Fact]
		public void StoreElement_FriendlyNameProvided()
		{
			var context = CreateProvider();
			var mockDatabase = Mock.Get(context.Database);
			mockDatabase.Setup(
				x => x.HashSet(
					It.Is<RedisKey>(k => k.Equals(RedisXmlRepository.RedisHashKey)),
					It.Is<RedisValue>(v => v.Equals("friendlyName")),
					It.IsAny<RedisValue>(),
					When.Always,
					CommandFlags.None)).Verifiable();
			context.Repository.StoreElement(XElement.Parse("<root />"), "friendlyName");
			mockDatabase.Verify();
		}

		[Fact]
		public void StoreElement_NullElement()
		{
			var context = CreateProvider();
			Assert.Throws<ArgumentNullException>(() => context.Repository.StoreElement(null, "friendlyName"));
		}

		[Fact]
		public void StoreElement_NullFriendlyName()
		{
			var context = CreateProvider();
			var mockDatabase = Mock.Get(context.Database);
			mockDatabase.Setup(
				x => x.HashSet(
					It.Is<RedisKey>(k => k.Equals(RedisXmlRepository.RedisHashKey)),
					It.Is<RedisValue>(v => !v.IsNullOrEmpty),
					It.IsAny<RedisValue>(),
					When.Always,
					CommandFlags.None)).Verifiable();
			context.Repository.StoreElement(XElement.Parse("<root />"), null);
			mockDatabase.Verify();
		}

		private static RedisTestContext CreateProvider()
		{
			// Set up a "fake Redis."
			var connection = new Mock<IConnectionMultiplexer>();
			connection.Setup(x => x.Configuration).Returns("connectionstring");
			var database = new Mock<IDatabase>();
			database.Setup(x => x.Database).Returns(123);
			connection.Setup(x => x.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(database.Object);

			// Use the "fake Redis" for the provider.
			var provider = new RedisXmlRepository(connection.Object, Mock.Of<ILogger<RedisXmlRepository>>());

			return new RedisTestContext
			{
				Connection = connection.Object,
				Database = database.Object,
				Repository = provider
			};
		}

		private class RedisTestContext
		{
			public IConnectionMultiplexer Connection { get; set; }

			public IDatabase Database { get; set; }

			public RedisXmlRepository Repository { get; set; }
		}
	}
}
