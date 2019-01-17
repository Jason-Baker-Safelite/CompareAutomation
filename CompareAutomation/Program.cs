using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

//The null.null file is used for new module "compares"

//Automation of the comparing of files derived by changes in the mainframe code theough BeyondCompare using command line
//Generating a report
//This script compares two files by name and generates an html report showing differences with context:
//text-report layout:side-by-side &
// options:ignore-unimportant,display-context &
// output-to:%3 output-options:html-color %1 %2
// https//www.scootersoftware.com/v4help/index.html?sample_scripts.html
//
// Assumptions:  file watcher will kick off once per hour
//               don't use same HLQ package name within the same hour
//               Always use initials for LLQ to allow emails to work

// command line example for 3 files:
// "C:\Program Files\Beyond Compare 4\BCompare.exe" C:\TEMP\SLG332JB_CPY_SLNAGSST_prod.TXT C:\TEMP\SLG332JB_CPY_SLNAGSST_dev.TXT C:\TEMP\null.txt1 C:\TEMP\OUTPUT.TXT


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
            FileInfo[] fileArray = dirInfo.GetFiles("*.txt");
            string cmdText = "C:\\Program Files\\Beyond Compare 4\\BCompare.exe";
            string nullFile = "null.null";
            string DevPackage;
            string ModuleType;
            string ModuleName;
            string Region;

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
                            DevFileName = nullFile,
                            ProdFileName = nullFile,
                            StgdFileName = nullFile
                        };
                        LoadFileName(Region, item, compareItem);
                        compareDictionary.Add(compareKey, compareItem);
                    }
                }
            }
            string directory = "C:\\TEMP\\";
            string finalArgs = "";

            foreach (var matchedItems in compareDictionary)
            {
                string cmdArgScript = "@\"C:\\Users\\Jason.Baker\\Projects\\GitHub\\CompareAutomation\\CompareAutomation\\comparescript.scr\"";
                string cmdArgDev = "\"C:\\TEMP\\" + matchedItems.Value.DevFileName + "\"";
                string cmdArgProd = "\"C:\\TEMP\\" + matchedItems.Value.ProdFileName + "\"";
                string cmdArgStgd = "\"C:\\TEMP\\" + matchedItems.Value.StgdFileName + "\"";
                string outputKey = matchedItems.Value.DevPackage + "_" + matchedItems.Value.ModuleType + "_" + matchedItems.Value.ModuleName + "_compare_prod_dev";
                string cmdArgOutput = "\"C:\\TEMP\\" + outputKey + ".html" + "\"";
                finalArgs = cmdArgScript + " " + cmdArgProd + " " + cmdArgDev + " " + cmdArgOutput;
                RunCommand(cmdText, finalArgs);
                if (matchedItems.Value.StgdFileName != nullFile)
                {
                    outputKey = matchedItems.Value.DevPackage + "_" + matchedItems.Value.ModuleType + "_" + matchedItems.Value.ModuleName + "_compare_dev_stgd";
                    cmdArgOutput = "\"C:\\TEMP\\" + outputKey + ".html" + "\"";
                    finalArgs = cmdArgScript + " " + cmdArgDev + " " + cmdArgStgd + " " + cmdArgOutput;
                    RunCommand(cmdText, finalArgs);
                }
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
                case stageRegion:
                    compareMatchItem.StgdFileName = item.Name;
                    break;
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
