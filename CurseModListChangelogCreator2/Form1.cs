using CurseModListChangelogCreator2.Properties;
using HtmlAgilityPack;
using ICSharpCode.SharpZipLib.Core;
using ICSharpCode.SharpZipLib.Zip;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CurseModListChangelogCreator2
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void btnOld_Click(object sender, EventArgs e)
        {
            SelectFile(dlgOldMod, txtOldModFile);
        }

        private void btnNew_Click(object sender, EventArgs e)
        {
            SelectFile(dlgNewMod, txtNewModFile);
        }

        private void SelectFile(OpenFileDialog dialog, TextBox textbox)
        {
            dialog.DefaultExt = "zip";
            dialog.Multiselect = false;
            dialog.CheckFileExists = true;
            dialog.CheckPathExists = true;
            dialog.InitialDirectory = GetDefaultPath();
            switch (dialog.ShowDialog())
            {
                case DialogResult.Yes:
                case DialogResult.OK:
                    textbox.Text = dialog.FileName;
                    break;
                case DialogResult.None:
                case DialogResult.Cancel:
                case DialogResult.Abort:
                case DialogResult.Retry:
                case DialogResult.Ignore:
                case DialogResult.No:
                default:
                    break;
            }
        }
        private string GetDefaultPath()
        {
            return System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                , @"Curse\Minecraft\Export");
        }

        private void btnCompare_Click(object sender, EventArgs e)
        {
            ClearLog();

            string oldPath = dlgOldMod.FileName;
            string newPath = dlgNewMod.FileName;

            //dont backup if the file is being modded.
            BackupTheFile(newPath);

            ModPackInformation oldVersionInformation = GetModPackData(oldPath);
            ModPackInformation newVersionInformation = GetModPackData(newPath);

            txtResult.Text = ComparePacks(oldVersionInformation, newVersionInformation);
            txtNewPack.Text = newVersionInformation.ModListHtml;

            
            if (cbRemoveCommonOverrides.Checked)
            {
                RemoveCommonAutoLoadedMods(newPath);
            }
            if (cbAddChangeLogToNew.Checked)
            {
                AddTextFileToZip(newPath, "changelog.html", txtResult.Text);
            }

        }

        private void RemoveCommonAutoLoadedMods(string path)
        {
           // List<string> itemsToRemove = new List<string>();
            using (ZipFile zf = new ZipFile(path))
            {
                zf.BeginUpdate();
                foreach (ZipEntry item in zf)
                {
                    if (ShouldDelete(item))
                    {
                     //   itemsToRemove.Add(item.Name);
                        zf.Delete(item);
                     
                    }
                    
                }
                zf.CommitUpdate();
            }
            //foreach (string item in itemsToRemove)
            //{
            //    using (ZipFile zf = new ZipFile(path))
            //    {
                   
                    
                    
            //    }
            //}
        }

        private bool ShouldDelete(ZipEntry item)
        {
            bool result = false;
            if (item.Name.StartsWith("overrides/mods") && item.IsFile)
            {
                result = true;
            }
            return result;
        }

        private void BackupTheFile(string path)
        {
            if (cbDoBackup.Checked && (cbAddChangeLogToNew.Checked || cbRemoveCommonOverrides.Checked))
            {
                DoBackup(path);
            }
            else if (cbDoBackup.Checked)
            {
                Log($"The file '{path}' is not being modified so there is no reason to back it up.");
            }
            else
            {
                Log("The Backup option was not selected.");
            }
        }

        private void AddTextFileToZip(string path, string fileName, string contents)
        {
            using (ZipFile zf = new ZipFile(path))
            {
                zf.BeginUpdate();
                zf.Add(new StringStaticDataSource(contents), fileName);
                zf.IsStreamOwner = false;
                zf.CommitUpdate();
                zf.Close();
            }
        }

        private void ClearLog()
        {
            txtProcessLog.Text = string.Empty;
        }
        private void Log(string message)
        {
            txtProcessLog.AppendText(message + Environment.NewLine);
        }
        private void DoBackup(string source)
        {
            string extension = "bak";
            string newPath = Path.ChangeExtension(source, extension);
            if (File.Exists(newPath))
            {
                string firstPart = newPath.Replace("." + extension, "");
                for (int i = 1; i < 10; i++)
                {
                    newPath = $"{firstPart}({i.ToString()}).{extension}";
                    if (!File.Exists(newPath))
                    {
                        break;
                    }
                }
                if (File.Exists(newPath))
                {
                    throw new IOException($"'{newPath}' already exists.  Please remove some old backups.");
                }
            }
            File.Copy(source, newPath);
            Log($"File {source} backed up to {newPath}");
        }

        private ModPackInformation GetModPackData(string path)
        {
            ModPackInformation result = new CurseModListChangelogCreator2.ModPackInformation();
            using (FileStream fs = File.OpenRead(path))
            using (ZipFile zf = new ZipFile(fs))
            {
                foreach (ZipEntry zipEntry in zf)
                {
                    if (!zipEntry.IsFile)
                    {
                        continue;           // Ignore directories
                    }
                    String entryFileName = zipEntry.Name;
                    if (entryFileName.Equals("manifest.json", StringComparison.InvariantCultureIgnoreCase))
                    {
                        using (Stream manifest = GetFile(zf, zipEntry))
                        {
                            HandleManifest(manifest, result);
                        }

                    }
                    else if (entryFileName.Equals("modlist.html", StringComparison.InvariantCultureIgnoreCase))
                    {
                        using (Stream modList = GetFile(zf, zipEntry))
                        {
                            HandleModList(modList, result);
                        }
                    }
                }
                zf.Close();
            }
            return result;
        }

        private void HandleModList(Stream modList, ModPackInformation info)
        {
            var doc = new HtmlAgilityPack.HtmlDocument();
            doc.Load(modList);
            HtmlNode root = doc.DocumentNode.FirstChild;//gets the ul element
            info.ModListHtml = doc.Text;
            foreach (var item in root.Elements("li"))
            {
                //this should be an li
                HtmlNode node = item.ChildNodes.FirstOrDefault();
                string name = node.InnerHtml;
                string url = node.GetAttributeValue("href", string.Empty);
                string projectid = url;
                if (url.StartsWith(Resources.CurseProjectUrl))
                {
                    projectid = url.Remove(0, Resources.CurseProjectUrl.Length);
                }
                ModVersion mod = GetMod(info, projectid);
                mod.Name = name;
                mod.Url = url;
            }
        }

        private void HandleManifest(Stream contents, ModPackInformation info)
        {
            ModPackInformation result = new ModPackInformation();
            using (StreamReader sr = new StreamReader(contents))
            using (JsonReader reader = new JsonTextReader(sr))
            {
                JsonSerializer serializer = new JsonSerializer();

                dynamic manifest = serializer.Deserialize(reader);
                info.MinecraftVersion = manifest.minecraft.version;
                info.Loader = manifest.minecraft.modLoaders[0].id;
                info.ModPackAuthor = manifest.author;
                info.ModPackVersion = manifest.version;
                info.ProjectId = manifest.projectID;

                foreach (dynamic file in manifest.files)
                {
                    ModVersion mod = GetMod(info, ((string)file.projectID));
                    mod.ModFileId = file.fileID;
                }
            }
        }

        private ModVersion GetMod(ModPackInformation info, string modId)
        {
            ModVersion result = info.Mods.FirstOrDefault(m => m.ModProjectId.Equals(modId, StringComparison.InvariantCultureIgnoreCase));
            if (result == null)
            {
                result = new ModVersion { ModProjectId = modId };
                info.Mods.Add(result);
            }
            return result;
        }
        private Stream GetFile(ZipFile zf, ZipEntry zipEntry)
        {

            byte[] buffer = new byte[4096];     // 4K is optimum
            Stream result = new MemoryStream();
            using (Stream zipStream = zf.GetInputStream(zipEntry))
            {
                // Manipulate the output filename here as desired.

                // Unzip file in buffered chunks. This is just as fast as unpacking to a buffer the full size
                // of the file, but does not waste memory.
                // The "using" will close the stream even if an exception occurs.
                StreamUtils.Copy(zipStream, result, buffer);
                result.Position = 0;
            }
            return result;
        }
        private string ComparePacks(ModPackInformation oldVersionInformation, ModPackInformation newVersionInformation)
        {
            StringBuilder result = new StringBuilder();
            result.AppendLine($"<p>Modpack version <a href='{Resources.CurseProjectUrl}{newVersionInformation.ProjectId}'>{newVersionInformation.ModPackVersion}</a><br/>");
            result.AppendLine($"Modpack Author {newVersionInformation.ModPackAuthor}</br>");
            if (!((string)oldVersionInformation.MinecraftVersion)
                .Equals((string)newVersionInformation.MinecraftVersion, StringComparison.InvariantCultureIgnoreCase))
            {
                result.AppendLine($"Minecraft version updated to {newVersionInformation.MinecraftVersion}<br/>");
            }

            if (!((string)oldVersionInformation.Loader)
               .Equals((string)newVersionInformation.Loader, StringComparison.InvariantCultureIgnoreCase))
            {
                result.AppendLine($"Loader updated to {newVersionInformation.Loader}<br/>");
            }
            result.AppendLine($"</p>");
            IList<ModVersion> removedMods = GetRemovedMods(oldVersionInformation.Mods, newVersionInformation.Mods);
            BuildList("Removed", removedMods, result);
            IList<ModVersion> addedMods = GetAddedMods(oldVersionInformation.Mods, newVersionInformation.Mods);
            BuildList("Added", addedMods, result);
            IList<ModVersion> updatedMods = GetUpdatedMods(oldVersionInformation.Mods, newVersionInformation.Mods);
            BuildList("Updated", updatedMods, result);
            result.AppendLine($"<p>Change log generated the Curse Mod List Change Log Creator by {Resources.Authors}<br/><a href=\"{Resources.GitHubUrlForTheProject}\">Get it from Github!</a></p>");
            return result.ToString();
        }

        private void BuildList(string title, IList<ModVersion> mods, StringBuilder result)
        {
            result.AppendLine($"<p><strong>{title}</strong>");
            result.AppendLine("<ul>");
            foreach (var mod in mods)
            {
                result.AppendLine($"<li><a href=\"{mod.Url}\">{mod.Name}</a></li>");
            }
            result.AppendLine("</ul></p>");
        }

        private IList<ModVersion> GetUpdatedMods(List<ModVersion> PreviousModVersion, List<ModVersion> newModVersions)
        {
            IEnumerable<string> oldModIds = PreviousModVersion.Select(p => p.ModProjectId);
            return newModVersions.Where(m =>
                PreviousModVersion.FirstOrDefault(p => p.ModProjectId.Equals(m.ModProjectId) && !p.ModFileId.Equals(m.ModFileId))
                != null)
                .ToList();
        }

        private IList<ModVersion> GetRemovedMods(List<ModVersion> oldVersionInformation, List<ModVersion> newVersionInformation)
        {
            return GetMissingMods(oldVersionInformation, newVersionInformation);
        }
        private IList<ModVersion> GetAddedMods(List<ModVersion> oldVersionInformation, List<ModVersion> newVersionInformation)
        {
            return GetMissingMods(newVersionInformation, oldVersionInformation);
        }
        private IList<ModVersion> GetMissingMods(List<ModVersion> baseListOfMods, List<ModVersion> expectedMods)
        {
            IEnumerable<string> modIds = expectedMods.Select(p => p.ModProjectId);
            return baseListOfMods.Where(m => !modIds.Contains(m.ModProjectId)).ToList();
        }

    }
}
