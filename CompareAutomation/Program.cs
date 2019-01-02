using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompareAutomation
{
    class Program
    {

        // look up how to prevent the BC window from displaying
        // build a file watcher program
        // if files are there, load them into a collection
        //                     parse the input file name for module, user, environment tag (dev vs. prod), Jira story number
        //                     string function to find next instance of the "_"
        //                     look for prod match of dev file
        //                           if match found, do the compare and set files as processed;
        //                                           otherwise, compare to an empty permanent file
        //                     email the compare based on username (build a table of userid <--> email address
        //                     delete the dev and prod files for that username
        //                     put the compare report in the proper Jira folder in SPO
        //                     wish list:  put a link of that compare report in SPO in the Jira story???
        //
        //"C:\\Program Files\\Beyond Compare 4\\BCompare.exe" \"TEMP\\comparescript.txt\" \"TEMP\\SLG332JB_CPY_SLNAGSST_dev.TXT\" \"TEMP\\SLG332JB_CPY_SLNAGSST_prod.TXT\" \"TEMP\Compare_SLNAGSST.html\"

        public enum FileBreakdown
        {
            devPackage = 0,
            moduleType = 1,
            moduleName = 2,
            environment = 3
        };

        static void Main(string[] args)
        {
            DirectoryInfo dirInfo = new DirectoryInfo("c:\\TEMP\\");
            FileInfo[] fileArray = dirInfo.GetFiles("?.txt");
            string cmdText = "C:\\Program Files\\Beyond Compare 4\\BCompare.exe";
            string nullFile = "null.txt1";
            string cmdArgs = "@\"C:\\TEMP\\comparescript.txt1\" \"C:\\TEMP\\SLG332JB_CPY_SLNAGSST_prod.TXT\" \"C:\\TEMP\\SLG332JB_CPY_SLNAGSST_dev.TXT\" \"C:\\TEMP\\Compare_SLNAGSST.html\"";
            //string cmdArgs = "@\"C:\\TEMP\\testscript.txt\" \"C:\\TEMP\\null.TXT1\" \"C:\\TEMP\\SLG332JB_CPY_SLNAGSST_prod.TXT\" \"C:\\TEMP\\Compare_SLNAGSST.html\"";
            //string cmdArgs = "@\"C:\\TEMP\\null.TXT1\" \"C:\\TEMP\\SLG332JB_CPY_SLNAGSST_prod.TXT\" \"C:\\TEMP\\Compare_SLNAGSST.html\"";
            string DevPackage;
            string ModuleType;
            string ModuleName;
            string Region;

            RunCommand(cmdText, cmdArgs);

            Dictionary<string, Compare> compareDictionary = new Dictionary<string, Compare>();
                        
            for (int arrayIndex = 0; arrayIndex < fileArray.Length; arrayIndex++)
            {
                FileInfo item = fileArray[arrayIndex];
                if (item.Name != nullFile)
                {
                    string[] parseString = item.Name.Split(new char[] { '_', '.' });
                    DevPackage = parseString[(int)FileBreakdown.devPackage];
                    ModuleType = parseString[(int)FileBreakdown.moduleType];
                    ModuleName = parseString[(int)FileBreakdown.moduleName];
                    Region = parseString[(int)FileBreakdown.environment];
                    string compareKey = DevPackage + ModuleType + ModuleName;

                    if (compareDictionary.ContainsKey(compareKey))
                    {
                        Compare compareMatchItem = compareDictionary[compareKey];
                        //compareDictionary[compareKey].Region
                        LoadFileName(Region, item, compareMatchItem);
                    }
                    else
                    {
                        Compare compareItem = new Compare()
                        {
                            ModuleName = ModuleName,
                            ModuleType = ModuleType,
                            Region = Region,
                            DevPackage = DevPackage,
                            Developer = "",
                            Processed = false,
                            DevFileName = "",
                            ProdFileName = ""
                        };
                        LoadFileName(Region, item, compareItem);
                        compareDictionary.Add(compareKey, compareItem);
                    }
                }
            }
            string directory = "C:\\TEMP\\";
            string finalArgs = "";
            //testscript.txt\" \"C:\\TEMP\\null.TXT1\" \"C:\\TEMP\\SLG332JB_CPY_SLNAGSST_prod.TXT\" \"C:\\TEMP\\Compare_SLNAGSST.html\"";
            foreach (var matchedItems in compareDictionary)
            {
                StringBuilder commandArguments = new StringBuilder();
                //commandArguments.Append(@"@\");
                commandArguments.Append(@"""");
                commandArguments.Append(directory);
                commandArguments.Append(matchedItems.Value.DevFileName);
                commandArguments.Append(@"""");
                commandArguments.Append(" ");
                commandArguments.Append(@"""");
                commandArguments.Append(directory);
                commandArguments.Append(matchedItems.Value.ProdFileName);
                commandArguments.Append(@"""");
                commandArguments.Append(" ");
                commandArguments.Append(@"""");
                commandArguments.Append(directory);
                commandArguments.Append(matchedItems.Key+".html");
                commandArguments.Append(@"""");
                commandArguments = commandArguments.Replace(@"\", @"\\");
                commandArguments.Insert(0, @"@\");
                finalArgs = commandArguments.ToString();
            }
            
            
            Console.ReadLine();
        }

        private static void LoadFileName(string Region, FileInfo item, Compare compareMatchItem)
        {
            const string devRegion = "dev";
            const string prodRegion = "prod";
            const string stageRegion = "stgd";

            switch (Region)
            {
                case devRegion:
                    compareMatchItem.DevFileName = item.Name;
                    break;
                case prodRegion:
                    compareMatchItem.ProdFileName = item.Name;
                    break;
                //case stageRegion:
                //    compareMatchItem.StageFileName = item.Name;
                //    break;
                default:
                    break;
            }
        }

        private static void RunCommand(string cmdText, string cmdArgs)
        {
            Process cmd = new Process();

            cmd.StartInfo.FileName = cmdText;
            cmd.StartInfo.RedirectStandardInput = true;
            cmd.StartInfo.RedirectStandardOutput = true;
            cmd.StartInfo.CreateNoWindow = false;
            cmd.StartInfo.UseShellExecute = false;
            cmd.StartInfo.Arguments = cmdArgs;

            cmd.Start();

            /* execute "dir" */

            //cmd.StandardInput.WriteLine("dir");
            cmd.StandardInput.Flush();
            cmd.StandardInput.Close();
            //Console.WriteLine(cmd.StandardOutput.ReadToEnd());
        }
    }
}
