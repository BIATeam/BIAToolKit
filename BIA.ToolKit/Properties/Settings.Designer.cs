﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace BIA.ToolKit.Properties {
    
    
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("Microsoft.VisualStudio.Editors.SettingsDesigner.SettingsSingleFileGenerator", "17.12.0.0")]
    internal sealed partial class Settings : global::System.Configuration.ApplicationSettingsBase {
        
        private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));
        
        public static Settings Default {
            get {
                return defaultInstance;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool BIATemplateLocalFolder {
            get {
                return ((bool)(this["BIATemplateLocalFolder"]));
            }
            set {
                this["BIATemplateLocalFolder"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool BIATemplateGitHub {
            get {
                return ((bool)(this["BIATemplateGitHub"]));
            }
            set {
                this["BIATemplateGitHub"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool CompanyFilesGit {
            get {
                return ((bool)(this["CompanyFilesGit"]));
            }
            set {
                this["CompanyFilesGit"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool CompanyFilesLocalFolder {
            get {
                return ((bool)(this["CompanyFilesLocalFolder"]));
            }
            set {
                this["CompanyFilesLocalFolder"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("D:\\...\\BIATemplate")]
        public string BIATemplateLocalFolderText {
            get {
                return ((string)(this["BIATemplateLocalFolderText"]));
            }
            set {
                this["BIATemplateLocalFolderText"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("https://.../_git/BIACompanyFiles")]
        public string CompanyFilesGitRepo {
            get {
                return ((string)(this["CompanyFilesGitRepo"]));
            }
            set {
                this["CompanyFilesGitRepo"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("D:\\...\\BIACompanyFiles")]
        public string CompanyFilesLocalFolderText {
            get {
                return ((string)(this["CompanyFilesLocalFolderText"]));
            }
            set {
                this["CompanyFilesLocalFolderText"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("D:\\...\\MyRootProjectPath")]
        public string CreateProjectRootFolderText {
            get {
                return ((string)(this["CreateProjectRootFolderText"]));
            }
            set {
                this["CreateProjectRootFolderText"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("CompanyName")]
        public string CreateCompanyName {
            get {
                return ((string)(this["CreateCompanyName"]));
            }
            set {
                this["CreateCompanyName"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("False")]
        public bool UseCompanyFile {
            get {
                return ((bool)(this["UseCompanyFile"]));
            }
            set {
                this["UseCompanyFile"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("")]
        public string CustomTemplates {
            get {
                return ((string)(this["CustomTemplates"]));
            }
            set {
                this["CustomTemplates"] = value;
            }
        }
        
        [global::System.Configuration.UserScopedSettingAttribute()]
        [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [global::System.Configuration.DefaultSettingValueAttribute("True")]
        public bool UpgradeRequired {
            get {
                return ((bool)(this["UpgradeRequired"]));
            }
            set {
                this["UpgradeRequired"] = value;
            }
        }
    }
}
