using System;
using FubuCore;
using FubuCore.CommandLine;
using FubuMVC.Core.Packaging;

namespace Fubu
{
    
    public class ManifestInput
    {
        public string Folder { get; set; }
        public bool OpenFlag { get; set; }
        public bool CreateFlag { get; set; } // creates, but does not override
        
        [FlagAlias("f")]
        public bool ForceFlag { get; set; } // forces the override

        public string AssemblyFlag { get; set; }

        [FlagAlias("class")]
        public string EnvironmentClassNameFlag { get; set; }        
    }

    [CommandDescription("Access an application manifest file")]
    public class ManifestCommand : FubuCommand<ManifestInput>
    {
        public override void Execute(ManifestInput input)
        {
            input.Folder = AliasCommand.AliasFolder(input.Folder);
            Execute(input, new FileSystem());
        }

        

        public virtual bool ApplyChanges(ManifestInput input, ApplicationManifest manifest)
        {
            var didChange = false;

            if (input.AssemblyFlag.IsNotEmpty())
            {
                manifest.EnvironmentAssembly = input.AssemblyFlag;
                didChange = true;
            }

            if (input.EnvironmentClassNameFlag.IsNotEmpty())
            {
                manifest.EnvironmentClassName = input.EnvironmentClassNameFlag;
                didChange = true;
            }

            return didChange;
        }

        public void Execute(ManifestInput input, IFileSystem fileSystem)
        {
            if (fileSystem.FileExists(input.Folder, ApplicationManifest.FILE))
            {
                if (input.CreateFlag)
                {
                    overwriteExistingFile(fileSystem, input);
                }
                else
                {
                    modifyAndListExistingManifest(fileSystem, input);
                }

                if (input.OpenFlag)
                {
                    fileSystem.OpenInNotepad(input.Folder, ApplicationManifest.FILE);
                }
            }
            else
            {
                if (input.CreateFlag)
                {
                    CreateManifest(fileSystem, input);
                }
                else
                {
                    WriteManifestCannotBeFound(input.Folder);
                }
            }
        }

        private void modifyAndListExistingManifest(IFileSystem fileSystem, ManifestInput input)
        {
            var manifest = fileSystem.LoadFromFile<ApplicationManifest>(input.Folder, ApplicationManifest.FILE);
            if (ApplyChanges(input, manifest))
            {
                persist(fileSystem, input, manifest);
            }

            WriteManifest(input, manifest);
        }

        private void overwriteExistingFile(IFileSystem fileSystem, ManifestInput input)
        {
            if (input.ForceFlag)
            {
                CreateManifest(fileSystem, input);
            }
            else
            {
                WriteCannotOverwriteFileWithoutForce(input.Folder);
            }
        }

        public virtual void CreateManifest(IFileSystem fileSystem, ManifestInput input)
        {
            var manifest = new ApplicationManifest();
            ApplyChanges(input, manifest);
            persist(fileSystem, input, manifest);

            WriteManifest(input, manifest);

            if (input.OpenFlag)
            {
                fileSystem.OpenInNotepad(input.Folder, ApplicationManifest.FILE);
            }
        }

        private void persist(IFileSystem fileSystem, ManifestInput input, ApplicationManifest manifest)
        {
            Console.WriteLine("");
            Console.WriteLine("Persisted changes to " + fileSystem.Combine(input.Folder, ApplicationManifest.FILE));
            Console.WriteLine("");

            fileSystem.PersistToFile(manifest, input.Folder, ApplicationManifest.FILE);
        }

        public virtual void WriteManifest(ManifestInput input, ApplicationManifest manifest)
        {
            var title = "Application Manifest for " + FileSystem.Combine(input.Folder, ApplicationManifest.FILE);
            var report = new TwoColumnReport(title);
            report.Add<ApplicationManifest>(x => x.EnvironmentAssembly, manifest);
            report.Add<ApplicationManifest>(x => x.EnvironmentClassName, manifest);
            report.Add<ApplicationManifest>(x => x.ConfigurationFile, manifest);

            report.Write();

            Console.WriteLine();
            Console.WriteLine();
            
            LinkCommand.ListCurrentLinks(input.Folder, manifest);
        }

        public virtual void WriteManifestCannotBeFound(string folder)
        {
            var file = FileSystem.Combine(folder, ApplicationManifest.FILE);
            Console.WriteLine("Application Manifest file at {0} does not exist", file);
        }

        public virtual void WriteCannotOverwriteFileWithoutForce(string folder)
        {
            var file = FileSystem.Combine(folder, ApplicationManifest.FILE);
            Console.WriteLine("File {0} already exists, use the '-f' flag to overwrite the existing file", file);
        }
    }
}