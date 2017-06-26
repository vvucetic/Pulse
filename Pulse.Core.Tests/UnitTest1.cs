using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Core.Common;
using Core;
using Pulse.SqlStorage;

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
            var client = new BackgroundJobClient(new SqlStorage.SqlStorage("db"));
            var id = client.Enqueue(() => Method(1, DateTime.UtcNow));
        }

        public void Method(int i, DateTime date)
        {

        }
    }
}
