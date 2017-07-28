using Pulse.SqlStorage.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pulse.SqlStorage
{
    public class TableNameMapper : ITableNameMapper
    {
        public Dictionary<Type, string> _mapping;
        public TableNameMapper(string schema)
        {
            _mapping = new Dictionary<Type, string>()
            {
                { typeof(JobConditionEntity), $"[{schema}].JobCondition" },
                { typeof(JobEntity), $"[{schema}].Job" },
                { typeof(QueueEntity), $"[{schema}].Queue" },
                { typeof(ScheduleEntity), $"[{schema}].Schedule" },
                { typeof(ServerEntity), $"[{schema}].Server" },
                { typeof(StateEntity), $"[{schema}].State" },
                { typeof(WorkerEntity), $"[{schema}].Worker" },
            };
        }
        public string GetTableName(Type type)
        {
            if (!_mapping.ContainsKey(type))
                throw new Exception($"Table name mapping for type '{type}' not set.");
            return _mapping[type];
        }
    }
}
