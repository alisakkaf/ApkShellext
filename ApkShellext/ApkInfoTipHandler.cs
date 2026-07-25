using ApkQuickReader;
using Microsoft.Win32;
using SharpShell.Attributes;
using SharpShell.Diagnostics;
using SharpShell.Extensions;
using SharpShell.ServerRegistration;
using SharpShell.SharpInfoTipHandler;
using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using ApkShellext.Properties;

namespace ApkShellext {

    [Guid("8D6C97F3-5E2A-4B6E-9E8D-2C1F5A6D9B4F")]
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.None)]
    [COMServerAssociation(AssociationType.ClassOfExtension, ".apk")]
    [COMServerAssociation(AssociationType.ClassOfExtension, ".xapk")]
    [COMServerAssociation(AssociationType.ClassOfExtension, ".ipa")]
    [COMServerAssociation(AssociationType.ClassOfExtension, ".appxbundle")]
    [COMServerAssociation(AssociationType.ClassOfExtension, ".appx")]
    public class ApkInfoTipHandler : SharpInfoTipHandler {
        /// <summary>
        /// Gets info for the selected item (SelectedItemPath).
        /// </summary>
        /// <param name="infoType">Type of info to return.</param>
        /// <param name="singleLine">if set to <c>true</c>, put the info in a single line.</param>
        /// <returns>
        /// Specified info for the selected file.
        /// </returns>
        protected override string GetInfo(RequestedInfoType infoType, bool singleLine) {
            try {
                Utility.Localize();
                string TipPattern = Utility.GetSetting("ToolTipPattern", NonLocalizeResources.strInfoTipDefault);
                bool isapk = SelectedItemPath.EndsWith(".apk");
                bool isipa = SelectedItemPath.EndsWith(".ipa");
                
                var cached = PackageCache.Get(SelectedItemPath);
                if (cached != null) {
                    return ApkContextMenu.ReplaceVariables(TipPattern, cached, SelectedItemPath);
                }

                using (AppPackageReader reader = AppPackageReader.Read(SelectedItemPath)) {
                    if (reader != null) {
                        PackageCache.Put(SelectedItemPath, reader);
                        return ApkContextMenu.ReplaceVariables(TipPattern, reader);
                    }
                }
                return Properties.Resources.strReadFileFailed;
            } catch (Exception ex) {
                Log("Error happend during GetInfo : " + ex.Message);
                return Properties.Resources.strReadFileFailed;
            }
        }

        [CustomRegisterFunction]
        public static void postDoRegister(Type type, RegistrationType registrationType) {
            Console.WriteLine("Registering " + type.FullName);

            #region Clean up older versions registry
            try {
                using (RegistryKey key = Registry.ClassesRoot.OpenSubKey(@"\CLSID\" +
                    type.GUID.ToRegistryString() + @"\InprocServer32")) {
                    if (key != null && key.GetSubKeyNames().Count() != 0) {
                        Console.WriteLine("Found old version in registry, cleaning up ...");
                        foreach (var k in key.GetSubKeyNames()) {
                            if (k != type.Assembly.GetName().Version.ToString()) {
                                Registry.ClassesRoot.DeleteSubKeyTree(@"\CLSID\" +
                        type.GUID.ToRegistryString() + @"\InprocServer32\" + k);
                            }
                        }
                    }
                }
            } catch (Exception e) {
                Logging.Error("Cleaning up older version but see exception. "
                     + e.Message);
            }
            #endregion
        }

        protected override void Log(string message){
            Utility.Log(this, Path.GetFileName(SelectedItemPath), message);
        }
    }
}
