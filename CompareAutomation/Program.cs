using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Mail;

namespace CompareAutomation
{
    class Program
    {
        public enum FileBreakdown
        {
            userIDIndex = 0,
            moduleTypeIndex = 1,
            moduleNameIndex = 2,
            storyIDIndex = 3,
            environmentIndex = 4
        };

        static void Main(string[] args)
        {
            string nullFile = "null.null";
            string userID;
            string moduleType;
            string moduleName;
            string storyID;
            string region;
            DirectoryInfo dirInfo = new DirectoryInfo(ConfigurationManager.AppSettings.Get("CompareFolder"));
            FileInfo[] fileArray = dirInfo.GetFiles("*.txt");
            string cmdText = ConfigurationManager.AppSettings.Get("BeyondCompareExe");
            string compareScript = ConfigurationManager.AppSettings.Get("CompareScript");
            string compareFolder = ConfigurationManager.AppSettings.Get("CompareFolder");
            string compareOutputFolder = ConfigurationManager.AppSettings.Get("CompareOutputFolder");
            string[] userKeyValues = ConfigurationManager.AppSettings.Get("EmailList").Split(new char[] { ';' });

            Dictionary<string, string> userEmailDictionary = new Dictionary<string, string>();
            foreach (string userKey in userKeyValues)
            {
                var userEmail = userKey.Split(new char[] { ':' });
                userEmailDictionary.Add(userEmail[0], userEmail[1]);
            }

            Dictionary<string, Compare> compareDictionary = new Dictionary<string, Compare>();

            for (int arrayIndex = 0; arrayIndex < fileArray.Length; arrayIndex++)
            {
                FileInfo item = fileArray[arrayIndex];
                if (item.Name != nullFile)
                {
                    string[] parseArray = item.Name.Split(new char[] { '_', '.' });
                    userID = parseArray[(int)FileBreakdown.userIDIndex];
                    moduleType = parseArray[(int)FileBreakdown.moduleTypeIndex];
                    moduleName = parseArray[(int)FileBreakdown.moduleNameIndex];
                    storyID = parseArray[(int)FileBreakdown.storyIDIndex];
                    region = parseArray[(int)FileBreakdown.environmentIndex];
                    string compareKey = userID + moduleType + moduleName;

                    if (userEmailDictionary.ContainsKey(userID))
                    {
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
                                UserID = userID,
                                Processed = false,
                                DevFileName = nullFile,
                                StoryID = storyID,
                                ProdFileName = nullFile,
                                StgdFileName = nullFile,
                                EmailAddress = userEmailDictionary[userID]
                            };
                            LoadFileName(region, item, compareItem);
                            compareDictionary.Add(compareKey, compareItem);
                        }
                    }
                }
            }
            string finalArgs = "";

            foreach (KeyValuePair<string, Compare> matchedItems in compareDictionary)
            {
                string userStoryAttachmentFolder = CheckForOutputFolder(compareOutputFolder, matchedItems.Value.StoryID);
                string cmdArgScript = "@\"" + compareScript + "\"";
                string cmdArgDev = "\"" + compareFolder + "\\" + matchedItems.Value.DevFileName + "\"";
                string cmdArgProd = "\"" + compareFolder + "\\" + matchedItems.Value.ProdFileName + "\"";
                string cmdArgStgd = "\"" + compareFolder + "\\" + matchedItems.Value.StgdFileName + "\"";
                string outputKey = matchedItems.Value.UserID + "_" + matchedItems.Value.ModuleType + "_" + matchedItems.Value.ModuleName + "_compare_prod_dev";
                string cmdArgOutput = "\"" + userStoryAttachmentFolder + "\\" + outputKey + ".html" + "\"";
                finalArgs = " /silent " + cmdArgScript + " " + cmdArgProd + " " + cmdArgDev + " " + cmdArgOutput;
                RunCommand(cmdText, finalArgs);
                if (matchedItems.Value.StgdFileName != nullFile)
                {
                    outputKey = matchedItems.Value.UserID + "_" + matchedItems.Value.ModuleType + "_" + matchedItems.Value.ModuleName + "_compare_dev_stgd";
                    cmdArgOutput = "\"" + userStoryAttachmentFolder + "\\" + outputKey + ".html" + "\"";
                    finalArgs = " /silent " + cmdArgScript + " " + cmdArgDev + " " + cmdArgStgd + " " + cmdArgOutput;
                    RunCommand(cmdText, finalArgs);
                }
                matchedItems.Value.Outputfile = cmdArgOutput;
                //MailMessage emailMessage = new MailMessage("JASON.BAKER@SAFELITE.COM", "JASON.BAKER@SAFELITE.COM", "Test compare", "This is a compare email");
            }
            //CleanUpCompareFolder(fileArray);
            //Console.ReadLine();
        }
        private static string CheckForOutputFolder(string checkOutputFolder, string checkstoryIDIndex)
        {
            DirectoryInfo dirOutputInfo = new DirectoryInfo(ConfigurationManager.AppSettings.Get("CompareOutputFolder"));
            DirectoryInfo[] outputFolderArray = dirOutputInfo.GetDirectories(checkstoryIDIndex, SearchOption.TopDirectoryOnly);
            string returnFolderName = checkstoryIDIndex;
            if (outputFolderArray.Count() == 0)
            {
                dirOutputInfo.CreateSubdirectory(returnFolderName);
            }
            return checkOutputFolder + "\\" + returnFolderName;
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
