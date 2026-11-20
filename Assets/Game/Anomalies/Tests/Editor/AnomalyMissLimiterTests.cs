using NUnit.Framework;

namespace Game.Anomalies.Tests
{
    public sealed class AnomalyMissLimiterTests
    {
        private static AnomalyMissLimiter Create() => new(3, 1f, 2f);

        [Test]
        public void ThirdMissWithinWindowStartsLockForTwoSeconds()
        {
            var limiter = Create();
            Assert.IsFalse(limiter.RegisterMiss(0f));
            Assert.IsFalse(limiter.RegisterMiss(0.4f));
            Assert.IsTrue(limiter.RegisterMiss(0.9f));
            Assert.IsTrue(limiter.IsLocked(2.8f));
            Assert.IsFalse(limiter.IsLocked(2.9f));
        }

        [Test]
        public void SlowMissesNeverLock()
        {
            var limiter = Create();
            for (var i = 0; i < 6; i++)
                Assert.IsFalse(limiter.RegisterMiss(i * 0.6f));
        }

        [Test]
        public void MissesDuringLockDoNotExtendIt()
        {
            var limiter = Create();
            limiter.RegisterMiss(0f);
            limiter.RegisterMiss(0.1f);
            limiter.RegisterMiss(0.2f);
            Assert.IsFalse(limiter.RegisterMiss(1f));
            Assert.IsFalse(limiter.IsLocked(2.3f));
        }
    }
}
