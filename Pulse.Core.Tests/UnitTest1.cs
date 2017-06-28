using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Pulse.SqlStorage;
using Pulse.Core.Common;
using System.Diagnostics;
using Pulse.Core.Server;
using System.Threading;

namespace Pulse.Core.Tests
{
    [TestClass]
    public class CoreTests
    {
        [TestMethod]
        public void JobFromExpression()
        {
            var job = Job.FromExpression(() => Method(4, DateTime.UtcNow));
        }

        [TestMethod]
        public void EnqueueTest()
        {
            GlobalConfiguration.Configuration.UseSqlServerStorage("db");
            var client = new BackgroundJobClient();
            for (int i = 0; i < 100; i++)
            {
                var id = client.Enqueue(() => Method(1, DateTime.UtcNow));

            }
        }

        [TestMethod]
        public void TestServer()
        {
            GlobalConfiguration.Configuration.UseSqlServerStorage("db");
            var server = new BackgroundJobServer();
            Thread.Sleep(TimeSpan.FromDays(1));
        }

        public void Method(int i, DateTime date)
        {
            Thread.Sleep(5000);
            Debug.WriteLine("Wooorks!");
            Debug.WriteLine("Nooo!");
            throw new Exception("Custom exception");
        }
    }
}
