using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Core.Common;

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

        public void Method(int i, DateTime date)
        {

        }
    }
}
