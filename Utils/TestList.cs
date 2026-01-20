using MyriaLib.Entities.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyriaLib.Utils
{
    public class TestList<T> : List<T>
    {
        public void Add(T item, bool test)
        {
            this.Add(item);
        }
    }
}
