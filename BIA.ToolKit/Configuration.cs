using BIA.ToolKit.Domain.Settings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace BIA.ToolKit
{
    public class Configuration
    {
        public string BIATemplatePath { get; private set; }
        public List<CustomRepoTemplate> customTemplates;

        public string RootCompanyFilesPath { get; private set; }
        public string AppFolderPath { get; private set; }
        public string TmpFolderPath { get; private set; }

        public bool BIATemplateLocalFolderIsChecked { get; private set; }
        public string BIaTemplateLocalFolderText { get; private set; }
        public bool UseCompanyFileIsChecked { get; private set; }
        public bool CompanyFilesLocalFolderIsChecked { get; private set; }
        public string CompanyFilesLocalFolderText { get; private set; }

        public Configuration()
        {
            BIATemplatePath = "";
            customTemplates = new List<CustomRepoTemplate>();
            RootCompanyFilesPath = "";
            AppFolderPath = System.Windows.Forms.Application.LocalUserAppDataPath;
            TmpFolderPath = Path.GetTempPath() + "BIAToolKit\\";
        }

        public bool RefreshBIATemplate(TabControl MainTab,
            bool biaTemplateLocalFolderIsChecked, string biaTemplateLocalFolderText, bool inSync)
        {
            BIATemplateLocalFolderIsChecked = biaTemplateLocalFolderIsChecked;
            BIaTemplateLocalFolderText = biaTemplateLocalFolderText;

            if (biaTemplateLocalFolderIsChecked)
            {
                //Use local folder
                BIATemplatePath = biaTemplateLocalFolderText;
                if (!inSync && !Directory.Exists(BIATemplatePath))
                {
                    MessageBox.Show("Error on biatemplate local folder :\r\nThe path " + BIATemplatePath + " do not exist.\r\n Correct it in config tab.");
                    return false;
                }
            }
            else
            {
                BIATemplatePath = AppFolderPath + "\\BIATemplate\\Repo";
                if (!inSync && !Directory.Exists(BIATemplatePath))
                {
                    MessageBox.Show("Error on biatemplate repo :\r\nThe path " + BIATemplatePath + " do not exist.\r\n Please synchronize the BIATemplate repository.");
                    return false;
                }
            }
            return true;
        }

        public bool RefreshCompanyFiles(TabControl MainTab,
            bool useCompanyFileIsChecked,
            bool companyFilesLocalFolderIsChecked, string companyFilesLocalFolderText, bool inSync)
         {
            UseCompanyFileIsChecked = useCompanyFileIsChecked;
            CompanyFilesLocalFolderIsChecked = companyFilesLocalFolderIsChecked;
            CompanyFilesLocalFolderText = companyFilesLocalFolderText;
            if (useCompanyFileIsChecked)
            {
                RootCompanyFilesPath = "";

                if (companyFilesLocalFolderIsChecked == true)
                {
                    RootCompanyFilesPath = companyFilesLocalFolderText;
                    if (!inSync && !Directory.Exists(RootCompanyFilesPath))
                    {
                        MessageBox.Show("Error on company files path local folder :\r\nThe path " + RootCompanyFilesPath + " do not exist.\r\n Correct it in config tab.");
                        return false;
                    }

                }
                else
                {
                    RootCompanyFilesPath = AppFolderPath + "\\BIACompanyFiles\\Repo";
                    if (!inSync && !Directory.Exists(RootCompanyFilesPath))
                    {
                        MessageBox.Show("Error on company files repo :\r\nThe path " + RootCompanyFilesPath + " do not exist.\r\n Please synchronize the company files repository.");
                        return false;
                    }
                }
            }
            return true;
        }
    }
}
