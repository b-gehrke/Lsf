using System;
using Lsf.Models;
using NUnit.Framework;

namespace Lsf.Test
{
    public class AppointmentTests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Conflicts_SameDate()
        {
            var a = new Appointment
            {
                Start = new DateTime(2019, 1, 1, 12, 0, 0),
                End = new DateTime(2019, 1, 1, 12, 30, 0)
            };

            var b = new Appointment
            {
                Start = new DateTime(2019, 1, 1, 12, 0, 0),
                End = new DateTime(2019, 1, 1, 12, 30, 0)
            };

            Assert.IsTrue(Appointment.Conflict(a, b));
            Assert.IsTrue(Appointment.Conflict(b, a));
        }

        [Test]
        public void Conflicts_NoIntersection()
        {
            var a = new Appointment
            {
                Start = new DateTime(2019, 1, 1, 12, 0, 0),
                End = new DateTime(2019, 1, 1, 12, 30, 0)
            };

            var b = new Appointment
            {
                Start = new DateTime(2019, 1, 1, 13, 0, 0),
                End = new DateTime(2019, 1, 1, 13, 30, 0)
            };

            Assert.IsFalse(Appointment.Conflict(a, b));
            Assert.IsFalse(Appointment.Conflict(b, a));
        }

        [Test]
        public void Conflicts_Adjacent()
        {
            var a = new Appointment
            {
                Start = new DateTime(2019, 1, 1, 12, 0, 0),
                End = new DateTime(2019, 1, 1, 12, 30, 0)
            };

            var b = new Appointment
            {
                Start = new DateTime(2019, 1, 1, 12, 30, 0),
                End = new DateTime(2019, 1, 1, 13, 00, 0)
            };

            Assert.IsFalse(Appointment.Conflict(a, b));
            Assert.IsFalse(Appointment.Conflict(b, a));
        }

        [Test]
        public void Conflicts_Contains()
        {
            var a = new Appointment
            {
                Start = new DateTime(2019, 1, 1, 12, 0, 0),
                End = new DateTime(2019, 1, 1, 14, 0, 0)
            };

            var b = new Appointment
            {
                Start = new DateTime(2019, 1, 1, 12, 30, 0),
                End = new DateTime(2019, 1, 1, 13, 00, 0)
            };

            Assert.IsTrue(Appointment.Conflict(a, b));
            Assert.IsTrue(Appointment.Conflict(b, a));
        }
    }
}