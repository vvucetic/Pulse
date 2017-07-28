using System;

namespace Pulse.SqlStorage
{
    public interface ITableNameMapper
    {
        string GetTableName(Type type);
    }
}