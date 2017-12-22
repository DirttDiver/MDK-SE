﻿using System;
using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace Malware.MDKModules
{
    /// <summary>
    /// Contains build options for a given MDK project
    /// </summary>
    public sealed class MDKProjectOptions : INotifyPropertyChanged
    {
        /// <summary>
        /// Loads options for the given project file.
        /// </summary>
        /// <param name="projectFileName">The file name of this project</param>
        /// <param name="projectName">The display name of this project</param>
        /// <returns></returns>
        public static MDKProjectOptions Load(string projectFileName, string projectName = null)
        {
            if (string.IsNullOrEmpty(projectFileName))
                throw new ArgumentException("Value cannot be null or empty.", nameof(projectFileName));
            var fileName = Path.GetFullPath(projectFileName);
            var name = projectName ?? Path.GetFileNameWithoutExtension(projectFileName);
            if (!File.Exists(fileName))
                return new MDKProjectOptions(fileName, name, false);
            var mdkOptionsFileName = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(fileName) ?? ".", @"mdk\mdk.options"));
            if (!File.Exists(mdkOptionsFileName))
                return new MDKProjectOptions(fileName, name, false);

            try
            {
                var document = XDocument.Load(mdkOptionsFileName);
                var root = document.Element("mdk");
                var version = Version.Parse(root?.Attribute("version")?.AsString() ?? "");
                var useManualGameBinPath = root?.Element("gamebinpath")?.Attribute("enabled")?.AsBoolean() ?? false;
                var gameBinPath = root?.Element("gamebinpath").AsString();
                var installPath = root?.Element("installpath").AsString();
                var outputPath = root?.Element("outputpath").AsString();
                var minify = root?.Element("minify")?.AsBoolean() ?? false;
                var ignoredFolders = root?.Elements("ignore", "folder").Select(e => e.AsString()).ToArray();
                var ignoredFiles = root?.Elements("ignore", "file").Select(e => e.AsString()).ToArray();

                MDKModuleReference composerModule;
                var composerElement = root?.Element("modules", "composer");
                if (composerElement != null)
                    composerModule = MDKModuleReference.FromXElement(composerElement);
                else
                    composerModule = null;
                MDKModuleReference publisherModule;
                var publisherElement = root?.Element("modules", "publisher");
                if (publisherElement != null)
                    publisherModule = MDKModuleReference.FromXElement(publisherElement);
                else
                    publisherModule = null;

                var result = new MDKProjectOptions(fileName, name, true)
                {
                    Version = version,
                    UseManualGameBinPath = useManualGameBinPath,
                    GameBinPath = gameBinPath,
                    InstallPath = installPath,
                    OutputPath = outputPath,
                    ComposerModule = composerModule,
                    PublisherModule = publisherModule,
#pragma warning disable 618
                    Minify = minify
#pragma warning restore 618
                };
                if (ignoredFolders != null)
                    foreach (var item in ignoredFolders)
                        result.IgnoredFolders.Add(item);
                if (ignoredFiles != null)
                    foreach (var item in ignoredFiles)
                        result.IgnoredFiles.Add(item);
                result.Commit();
                return result;
            }
            catch (Exception e)
            {
                throw new MDKProjectOptionsException($"An error occurred while attempting to load project options for {fileName}.", e);
            }
        }

        bool _minify;
        bool _hasChanges;
        string _gameBinPath;
        string _installPath;
        bool _useManualGameBinPath;
        string _outputPath;
        string[] _ignoredFilesCache;
        string[] _ignoredFoldersCache;
        string _baseDir;
        Version _version;

        MDKProjectOptions(string fileName, string name, bool isValid)
        {
            FileName = fileName;
            if (fileName != null)
                _baseDir = Path.GetFullPath(Path.GetDirectoryName(fileName) ?? ".");
            Name = name;
            IsValid = isValid;
            IgnoredFolders.CollectionChanged += OnIgnoredFoldersChanged;
            IgnoredFiles.CollectionChanged += OnIgnoredFilesChanged;
        }

        /// <inheritdoc />
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// The MDK project version.
        /// </summary>
        [Required]
        public Version Version
        {
            get => _version;
            set
            {
                if (value == _version)
                    return;
                _version = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets the name of the project
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Determines whether changes have been made to the options for this project
        /// </summary>
        public bool HasChanges
        {
            get => _hasChanges;
            private set
            {
                if (value == _hasChanges)
                    return;
                _hasChanges = value;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Determines whether this is a valid MDK project
        /// </summary>
        public bool IsValid { get; }

        /// <summary>
        /// Gets the project file name
        /// </summary>
        public string FileName { get; }

        /// <summary>
        /// Determines whether <see cref="GameBinPath"/> should be used, or the default value
        /// </summary>
        public bool UseManualGameBinPath
        {
            get => _useManualGameBinPath;
            set
            {
                if (value == _useManualGameBinPath)
                    return;
                _useManualGameBinPath = value;
                HasChanges = true;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Determines the path to the game's installed binaries.
        /// </summary>
        [Required(AllowEmptyStrings = false)]
        public string GameBinPath
        {
            get => _gameBinPath;
            set
            {
                if (value == _gameBinPath)
                    return;
                _gameBinPath = value ?? "";
                HasChanges = true;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Determines the installation path for the extension.
        /// </summary>
        [Required(AllowEmptyStrings = false)]
        public string InstallPath
        {
            get => _installPath;
            set
            {
                if (value == _installPath)
                    return;
                _installPath = value ?? "";
                HasChanges = true;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Determines the output path where the finished deployed script will be stored
        /// </summary>
        [Required(AllowEmptyStrings = false)]
        public string OutputPath
        {
            get => _outputPath;
            set
            {
                if (value == _outputPath)
                    return;
                _outputPath = value ?? "";
                HasChanges = true;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// Gets or sets the ID of an optional composer module.
        /// </summary>
        public MDKModuleReference ComposerModule { get; set; }

        /// <summary>
        /// Gets or sets the ID of an optional publisher module.
        /// </summary>
        public MDKModuleReference PublisherModule { get; set; }

        /// <summary>
        /// Determines whether the script generated from this project should be run through the minifier
        /// </summary>
        [Obsolete("This member is obsolete and ignored from 1.1.0 forward. Please use the composer module instead.")]
        public bool Minify
        {
            get => _minify;
            set
            {
                if (value == _minify)
                    return;
                _minify = value;
                HasChanges = true;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// A list of folders which code will not be included in neither analysis nor deployment
        /// </summary>
        public ObservableCollection<string> IgnoredFolders { get; } = new ObservableCollection<string>();

        /// <summary>
        /// A list of files which code will not be included in neither analysis nor deployment
        /// </summary>
        public ObservableCollection<string> IgnoredFiles { get; } = new ObservableCollection<string>();

        string FullyQualifiedFile(string path)
        {
            if (Path.IsPathRooted(path))
                return Path.GetFullPath(path);
            return Path.GetFullPath(Path.Combine(_baseDir, path));
        }

        string FullyQualifiedFolder(string path)
        {
            path = FullyQualifiedFile(path);
            if (path.EndsWith("\\"))
                return path;
            return path + "\\";
        }

        void OnIgnoredFilesChanged(object sender, NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
        {
            _ignoredFilesCache = null;
        }

        void OnIgnoredFoldersChanged(object sender, NotifyCollectionChangedEventArgs notifyCollectionChangedEventArgs)
        {
            _ignoredFoldersCache = null;
        }

        /// <summary>
        /// Commits all changes without saving. <see cref="HasChanges"/> will be false after this. This method is not required when calling <see cref="Save"/>.
        /// </summary>
        public void Commit()
        {
            HasChanges = true;
        }

        /// <summary>
        /// Saves the options of this project
        /// </summary>
        /// <remarks>Warning: If the originating project is not saved first, these changes might be overwritten.</remarks>
        public void Save(IMDK mdk)
        {
            if (mdk == null)
                throw new ArgumentNullException(nameof(mdk));
            if (!TryValidate(out var results))
                throw new MDKProjectOptionsException(string.Join(Environment.NewLine, results.Select(CreateExceptionMessage)));

            try
            {
                var mdkOptionsFileName = new FileInfo(Path.GetFullPath(Path.Combine(Path.GetDirectoryName(FileName) ?? ".", @"mdk\mdk.options")));
                if (!mdkOptionsFileName.Directory?.Exists ?? true)
                    mdkOptionsFileName.Directory?.Create();
                XDocument document;
                XElement gameBinPathElement = null;
                XAttribute useManualGameBinPathAttribute = null;
                XElement installPathElement = null;
                XElement outputPathElement = null;
                XElement ignoreElement = null;
                XElement root;
                XElement modulesElement = null;
                Version = mdk.Options.Version;
                if (!mdkOptionsFileName.Exists)
                {
                    document = new XDocument(new XDeclaration("1.0", "UTF-8", "yes"));
                    root = new XElement("mdk", new XAttribute("version", Version));
                    document.Add(root);
                }
                else
                {
                    document = XDocument.Load(mdkOptionsFileName.FullName);
                    root = document.Element("mdk");
                    // ReSharper disable once JoinNullCheckWithUsage
                    if (root == null)
                        throw new InvalidOperationException("Not a valid MDK Options File");

                    gameBinPathElement = root.Element("gamebinpath");
                    useManualGameBinPathAttribute = gameBinPathElement?.Attribute("enabled");
                    installPathElement = root.Element("installpath");
                    outputPathElement = root.Element("outputpath");
                    ignoreElement = root.Element("ignore");
                    modulesElement = root.Element("modules");
                }

                if (gameBinPathElement == null)
                {
                    gameBinPathElement = new XElement("gamebinpath");
                    root.Add(gameBinPathElement);
                }
                if (useManualGameBinPathAttribute == null)
                {
                    useManualGameBinPathAttribute = new XAttribute("enabled", "");
                    gameBinPathElement.Add(useManualGameBinPathAttribute);
                }

                if (installPathElement == null)
                {
                    installPathElement = new XElement("installpath");
                    root.Add(installPathElement);
                }
                if (outputPathElement == null)
                {
                    outputPathElement = new XElement("outputpath");
                    root.Add(outputPathElement);
                }
                if (ignoreElement == null && IgnoredFolders.Count > 0)
                {
                    ignoreElement = new XElement("ignore");
                    root.Add(ignoreElement);
                }
                if (modulesElement == null && ComposerModule != null || PublisherModule != null)
                {
                    modulesElement = new XElement("modules");
                    root.Add(modulesElement);
                }

                gameBinPathElement.Value = GameBinPath.TrimEnd('\\');
                useManualGameBinPathAttribute.Value = UseManualGameBinPath ? "yes" : "no";
                installPathElement.Value = InstallPath.TrimEnd('\\');
                outputPathElement.Value = OutputPath.TrimEnd('\\');
                ignoreElement?.RemoveNodes();
                if (ignoreElement != null)
                {
                    foreach (var folder in IgnoredFolders)
                        ignoreElement.Add(new XElement("folder", folder));
                    foreach (var file in IgnoredFiles)
                        ignoreElement.Add(new XElement("file", file));
                }
                modulesElement?.RemoveNodes();
                if (modulesElement != null)
                {
                    if (ComposerModule != null)
                        modulesElement.Add(ComposerModule.ToXElement("composer"));
                    if (PublisherModule != null)
                        modulesElement.Add(PublisherModule.ToXElement("publisher"));
                }

                HasChanges = false;

                document.Save(mdkOptionsFileName.FullName, SaveOptions.OmitDuplicateNamespaces);
            }
            catch (Exception e)
            {
                throw new MDKProjectOptionsException($"An error occurred while attempting to save project options to {FileName}.", e);
            }
        }

        string CreateExceptionMessage(ValidationResult validationResult)
        {
            return $"{string.Join(",", validationResult.MemberNames)}: {validationResult.ErrorMessage}";
        }

        void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Returns the actual game bin path to use, depending on the settings in this project.
        /// </summary>
        /// <param name="defaultPath">The default path to use when <see cref="UseManualGameBinPath"/> is <c>false</c></param>
        /// <returns></returns>
        public string GetActualGameBinPath(string defaultPath)
        {
            if (UseManualGameBinPath)
                return Path.GetFullPath(string.IsNullOrEmpty(GameBinPath) ? defaultPath : GameBinPath);
            return Path.GetFullPath(defaultPath);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return Name;
        }

        /// <summary>
        /// Determines whether the given file path is within one of the ignored folders or files.
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public bool IsIgnoredFilePath(string filePath)
        {
            filePath = Path.GetFullPath(filePath);

            if (_ignoredFilesCache == null)
                _ignoredFilesCache = IgnoredFiles.Select(FullyQualifiedFile).ToArray();
            if (_ignoredFilesCache.Any(path => filePath.Equals(path, StringComparison.CurrentCultureIgnoreCase)))
                return true;

            if (_ignoredFoldersCache == null)
                _ignoredFoldersCache = IgnoredFolders.Select(FullyQualifiedFolder).ToArray();
            if (_ignoredFoldersCache.Any(path => filePath.StartsWith(path, StringComparison.CurrentCultureIgnoreCase)))
                return true;

            return false;
        }

        /// <summary>
        /// Validates the options to see if everything is in working order
        /// </summary>
        /// <param name="results"></param>
        /// <returns></returns>
        public bool TryValidate(out ImmutableArray<ValidationResult> results)
        {
            var context = new ValidationContext(this);
            var resultsBuilder = ImmutableArray.CreateBuilder<ValidationResult>();
            var isValid = Validator.TryValidateObject(this, context, resultsBuilder);
            if (!isValid)
            {
                results = resultsBuilder.MoveToImmutable();
                return false;
            }
            results = ImmutableArray<ValidationResult>.Empty;
            return true;
        }
    }
}
