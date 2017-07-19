using NPoco;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.SqlStorage.Entities
{
    [TableName("Server")]
    [PrimaryKey(new[] { "Id" }, AutoIncrement = false)]
    public class ServerEntity
    {
        public string Id { get; set; }

        public string Data { get; set; }

        public DateTime LastHeartbeat { get; set; }
    }
}
