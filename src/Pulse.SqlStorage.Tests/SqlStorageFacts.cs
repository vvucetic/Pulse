using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Pulse.SqlStorage.Tests
{
    public class SqlStorageFacts
    {
        private readonly SqlServerStorageOptions _options;

        public SqlStorageFacts()
        {
            _options = new SqlServerStorageOptions() { PrepareSchemaIfNecessary = false };
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenConnectionStringIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new SqlStorage((string)null));

            Assert.Equal("connectionStringName", exception.ParamName);
        }

        [Fact]
        public void Ctor_ThrowsAnException_WhenOptionsIsNull()
        {
            var exception = Assert.Throws<ArgumentNullException>(
                () => new SqlStorage("", null));

            Assert.Equal("options", exception.ParamName);
        }

        //[Fact]
        //public void Ctor_SetsSchemaNameToAllPocos_WhenInitialized()
        //{
        //    var sqlStorage =  new SqlStorage("test", new SqlServerStorageOptions { SchemaName = "NewSchema" });

        //    var db = CustomDatabaseFactory.DbFactory.GetDatabase();
        //    var mappers = db.Mappers;

        //    //Assert.All(mappers, t=>t.)

        //    //Assert.Equal("options", exception.ParamName); CustomDatabaseFactory.DbFactory.GetDatabase();
        //}
    }
}
