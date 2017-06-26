using NPoco;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.SqlStorage.Entities
{
    [TableName("State")]
    [PrimaryKey("Id", AutoIncrement = true)]
    public class StateEntity
    {
        public int Id { get; set; }

        public int JobId { get; set; }

        public string Name { get; set; }

        public string Reason { get; set; }

        public DateTime CreatedAt { get; set; }

        public string Data { get; set; }
    }
}
