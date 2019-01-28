using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;

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
        //                     parse the input file name for module, user, environmentIndex tag (dev vs. prod), Jira story number
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
            devPackageIndex = 0,
            moduleTypeIndex = 1,
            moduleNameIndex = 2,
            storyIDIndex = 3,
            environmentIndex = 4
        };

        static void Main(string[] args)
        {
            DirectoryInfo dirInfo = new DirectoryInfo(ConfigurationManager.AppSettings.Get("CompareFolder"));
            FileInfo[] fileArray = dirInfo.GetFiles("*.txt");
            string cmdText = ConfigurationManager.AppSettings.Get("BeyondCompareExe");
            string compareScript = ConfigurationManager.AppSettings.Get("CompareScript");
            string compareFolder = ConfigurationManager.AppSettings.Get("CompareFolder");
            string compareOutputFolder = ConfigurationManager.AppSettings.Get("CompareOutputFolder");
            string nullFile = "null.null";
            string devPackage;
            string moduleType;
            string moduleName;
            string storyID;
            string region;

            Dictionary<string, Compare> compareDictionary = new Dictionary<string, Compare>();

            for (int arrayIndex = 0; arrayIndex < fileArray.Length; arrayIndex++)
            {
                FileInfo item = fileArray[arrayIndex];
                if (item.Name != nullFile)
                {
                    string[] parseString = item.Name.Split(new char[] { '_', '.' });
                    devPackage = parseString[(int)FileBreakdown.devPackageIndex];
                    moduleType = parseString[(int)FileBreakdown.moduleTypeIndex];
                    moduleName = parseString[(int)FileBreakdown.moduleNameIndex];
                    storyID = parseString[(int)FileBreakdown.storyIDIndex];
                    region = parseString[(int)FileBreakdown.environmentIndex];
                    string compareKey = devPackage + moduleType + moduleName;

                    if (compareDictionary.ContainsKey(compareKey))
                    {
                        Compare compareMatchItem = compareDictionary[compareKey];
                        LoadFileName(region, item, compareMatchItem);
                    }
                    else
                    {
                        Compare compareItem = new Compare()
                        {
                            ModuleName = moduleName,
                            ModuleType = moduleType,
                            Region = region,
                            DevPackage = devPackage,
                            Developer = "",
                            Processed = false,
                            DevFileName = nullFile,
                            StoryID = storyID,
                            ProdFileName = nullFile,
                            StgdFileName = nullFile
                        };
                        LoadFileName(region, item, compareItem);
                        compareDictionary.Add(compareKey, compareItem);
                    }
                }
            }
            string finalArgs = "";

            foreach (var matchedItems in compareDictionary)
            {
                string userStoryAttachmentFolder = CheckForOutputFolder(compareOutputFolder, matchedItems.Value.StoryID);
                string cmdArgScript = "@\"" + compareScript + "\"";
                string cmdArgDev = "\"" + compareFolder + "\\" + matchedItems.Value.DevFileName + "\"";
                string cmdArgProd = "\"" + compareFolder + "\\" + matchedItems.Value.ProdFileName + "\"";
                string cmdArgStgd = "\"" + compareFolder + "\\" + matchedItems.Value.StgdFileName + "\"";
                string outputKey = matchedItems.Value.DevPackage + "_" + matchedItems.Value.ModuleType + "_" + matchedItems.Value.ModuleName + "_compare_prod_dev";
                string cmdArgOutput = "\"" + userStoryAttachmentFolder + "\\" + outputKey + ".html" + "\"";
                finalArgs = " /silent " + cmdArgScript + " " + cmdArgProd + " " + cmdArgDev + " " + cmdArgOutput;
                RunCommand(cmdText, finalArgs);
                if (matchedItems.Value.StgdFileName != nullFile)
                {
                    outputKey = matchedItems.Value.DevPackage + "_" + matchedItems.Value.ModuleType + "_" + matchedItems.Value.ModuleName + "_compare_dev_stgd";
                    cmdArgOutput = "\"" + userStoryAttachmentFolder + "\\" + outputKey + ".html" + "\"";
                    finalArgs = " /silent " + cmdArgScript + " " + cmdArgDev + " " + cmdArgStgd + " " + cmdArgOutput;
                    RunCommand(cmdText, finalArgs);
                }
            }
            //CleanUpCompareFolder(fileArray);
            Console.ReadLine();
        }
        private static string CheckForOutputFolder(string checkOutputFolder, string checkstoryIDIndex)
        {
            DirectoryInfo dirOutputInfo = new DirectoryInfo(ConfigurationManager.AppSettings.Get("CompareOutputFolder"));
            DirectoryInfo[] outputArray = dirOutputInfo.GetDirectories(checkstoryIDIndex, SearchOption.TopDirectoryOnly);
            if (outputArray.Count() == 0)
            {
                // create
            } 
            else
            {
                // update
            }
            return outputArray[0].FullName;
        }
        private static void CleanUpCompareFolder(FileInfo[] fileArray)
        {
            foreach (FileInfo fileName in fileArray)
            {
                fileName.Delete();
            }
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
            cmd.StandardInput.Flush();
            cmd.StandardInput.Close();
        }
    }
}
