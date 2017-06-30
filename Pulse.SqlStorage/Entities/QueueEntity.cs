using NPoco;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.SqlStorage.Entities
{
    [TableName("Queue")]
    [PrimaryKey(new[] { "Id" }, AutoIncrement = true)]
    public class QueueEntity
    {
        public int Id { get; set; }

        public int JobId { get; set; }

        public string Queue { get; set; }

        public DateTime? FetchedAt { get; set; }

        public string WorkerId { get; set; }
    }
}
