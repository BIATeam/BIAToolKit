1. Ensure to have valid code signature certificate installed on your computer trusted by your company or yourself
2. Set in the Package.appxmanifest the identity of the publisher that must match to the subject of your trusted certificate
<Identity
    Publisher="CN=Your company, OU=0000, O=Company, C=US" />
3. Build the Bia.Toolkit.Package project
4. Right click on Bia.Toolkit.Package project -> Publish -> Create App Packages...
5. Choose Sideloading with automatic updates, then Next
6. Skip signing, then Next
7. Set the folder where the package will be created, the version, choose "Never" for generate app bundle, select x86 architecture and release configuration, then Next
8. Set the installer location where the installer package will be distributed to users, choose automatic update strategy, then Create
9. Wait for package creation, then Close
10. Open the created package folder
11. Identify the SignTool.exe to use https://learn.microsoft.com/en-us/windows/msix/package/sign-app-package-using-signtool#prerequisites
12. Run the following command to sign the MSIX installer of your new version with your trusted certificate : 
[SIGNTOOL-PATH] sign /fd SHA256 /n [TRUSTED-CERTIFICATE-NAME] [MSIX-PATH]
13. Copy the content of the package folder to the installer location

*** WARNING ***
Do not commit modifications for Package.appxmanifest and Bia.ToolKit.Package.wapproj that contains sensitive information