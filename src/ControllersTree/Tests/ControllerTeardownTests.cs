using System;
using System.Threading;
using NUnit.Framework;
using Playtika.Controllers;
using Playtika.Controllers.Substitute;

namespace UnitTests.Controllers
{
    [TestFixture]
    public class ControllerTeardownTests
    {
        private const string OnStartFailed = "OnStart failed";
        private const string OnStopFailed = "OnStop failed";

        private CancellationTokenSource _cancellationTokenSource;

        [SetUp]
        public void SetUp()
        {
            _cancellationTokenSource = new CancellationTokenSource();
        }

        [TearDown]
        public void TearDown()
        {
            _cancellationTokenSource.Cancel();
            _cancellationTokenSource.Dispose();
        }

        [Test]
        public void Execute_OnStartAndOnStopThrow_FailedChildLeavesTree()
        {
            var root = Launch(factory => new ThrowingController(factory, throwOnStart: true, throwOnStop: true));

            Assert.Throws<AggregateException>(() => root.Execute<ThrowingController>());

            Assert.AreEqual(nameof(TestRootController), root.DumpControllersTree());
        }

        private TestRootController Launch<T>(Func<IControllerFactory, T> createController)
            where T : IController
        {
            var factory = new SubstituteControllerFactory();
            factory.AddInstance(createController(factory));
            var root = new TestRootController(factory);
            root.LaunchTree(_cancellationTokenSource.Token);
            return root;
        }

        private class ThrowingController : ControllerBase
        {
            private readonly bool _throwOnStart;
            private readonly bool _throwOnStop;

            public ThrowingController(IControllerFactory factory, bool throwOnStart, bool throwOnStop)
                : base(factory)
            {
                _throwOnStart = throwOnStart;
                _throwOnStop = throwOnStop;
            }

            protected override void OnStart()
            {
                if (_throwOnStart)
                {
                    throw new InvalidOperationException(OnStartFailed);
                }
            }

            protected override void OnStop()
            {
                if (_throwOnStop)
                {
                    throw new InvalidOperationException(OnStopFailed);
                }
            }
        }
    }
}
