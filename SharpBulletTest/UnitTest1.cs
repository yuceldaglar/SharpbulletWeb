using System;
using System.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SharpBullet;
using SharpBullet.Entities;
using SharpBullet.OAL;

namespace SharpBulletTest
{
    [TestClass]
    public class UnitTest1
    {
        [TestInitialize]
        public void Initialize()
        {
            var dbType = ConfigurationManager.AppSettings["dbType"];
            var connection = ConfigurationManager.AppSettings["connection"];

            Transaction.SetConnection(dbType, connection);

            SharpBullet.OAL.Schema.SchemaHelper.Migrate(
                typeof(SbEntity), true, Transaction.Instance, this.GetType().Assembly);
        }

        [TestCleanup]
        public void Cleanup()
        {
            Transaction.Instance.ExecuteNonQuery(
                "drop table Book");
        }

        private void EmptyBooksTable()
        {
            Transaction.Instance.ExecuteNonQuery(
                "truncate table Book");
        }

        [TestMethod]
        public void TestConnection()
        {
            var x = Transaction.Instance.ExecuteScalarI("select 5");            
        }

        [TestMethod]
        public void InsertTest1()
        {
            EmptyBooksTable();

            var b = new Book()
            {
                Title = "Hello world",
                Author = "Yucel Daglar",
                Year = 2019
            };

            b.Insert();
            Assert.AreNotEqual(0, b.Id);
        }

        [TestMethod]
        public void UpdateTest1()
        {
            EmptyBooksTable();

            var b = new Book()
            {
                Title = "Hello world",
                Author = "Yucel Daglar",
                Year = 2019
            };

            b.Insert();

            b.Year = 2020;
            b.Update();

            var x = Transaction.Instance.Read<Book>(b.Id);
            Assert.AreEqual(2020, x.Year);
        }
    }
}