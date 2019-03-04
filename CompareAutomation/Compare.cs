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
        public string Region { get; set; }
        public string ModuleType { get; set; }
        public string UserID { get; set; }
        public string StoryID { get; set; }
        public bool Processed { get; set; }
        public string DevFileName { get; set; }
        public string ProdFileName { get; set; }
        public string StgdFileName { get; set; }
        public string EmailAddress { get; set; }
        public string Outputfile { get; set; }
    }
}
