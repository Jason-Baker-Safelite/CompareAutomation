using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompareAutomation
{
    public class Compare
    {
        public Compare()
        {
        }
        public string ModuleName { get; set; }
        public string Developer { get; set; }
        public string Region { get; set; }
        public string ModuleType { get; set; }
        public string DevPackage { get; set; }
        public bool Processed { get; set; }
        public string DevFileName { get; set; }
        public string ProdFileName { get; set; }
        public string StgdFileName { get; set; }
    }
}
