using Myria.Lib.Core.Entities.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Myria.Lib.Core.Utils
{
    public class TestList<T> : List<T>
    {
        public void Add(T item, bool test)
        {
            this.Add(item);
        }
    }
}
