using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Server
{
    internal interface IBackgroundProcessWrapper : IBackgroundProcess
    {
        IBackgroundProcess InnerProcess { get; }
    }
}   
