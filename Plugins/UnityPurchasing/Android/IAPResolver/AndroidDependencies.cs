// SelfDeclaredAndroidDependencies v3
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEditor;
using UnityEditor.Android;
using UnityEngine;

namespace Unity.SelfDeclaredAndroidDependencies.Editor
{
    [AttributeUsage(AttributeTargets.Class)]
    class SelfDeclaredAndroidDependenciesAttribute : Attribute
    {
    }

    [SelfDeclaredAndroidDependencies]
    abstract class AndroidDependencies : IPostGenerateGradleAndroidProject
    {
        const string k_AndroidDependenciesDisableKey = "SelfDeclaredAndroidDependenciesDisabled";

        public virtual int callbackOrder { get; } = 1;

        /// <summary>
        /// The name of the dependant. This is used to identify the section in the build.gradle file.
        /// </summary>
        public abstract string DependantName { get; }

        /// <summary>
        /// The list of dependencies to be added to the build.gradle file.
        /// format: "group:name:version"
        /// </summary>
        public abstract List<string> Dependencies { get; }

        /// <summary>
        /// The list of repositories to be added to the settings.gradle file.
        /// </summary>
        public abstract List<string> Repositories { get; }

        /// <summary>
        /// The list of properties to be added to the gradle.properties file.
        /// </summary>
        public abstract List<string> GradleProperties { get; }

        /// <summary>
        /// The line that marks the start of the dependencies section in the build.gradle file.
        /// </summary>
        protected string DependenciesLineStart => $"// Dependencies for \"{DependantName}\". This section is automatically generated.";

        /// <summary>
        /// The line that marks the end of the dependencies section in the build.gradle file.
        /// </summary>
        protected string DependenciesLineEnd => $"// End of dependencies for \"{DependantName}\".";

        /// <summary>
        /// The line that marks the start of the repositories section in the settings.gradle file.
        /// </summary>
        protected string RepositoriesLineStart => $"// Repositories for \"{DependantName}\". This section is automatically generated.";

        /// <summary>
        /// The line that marks the end of the repositories section in the settings.gradle file.
        /// </summary>
        protected string RepositoriesLineEnd => $"// End of repositories for \"{DependantName}\".";

        /// <summary>
        /// The line that marks the start of the properties section in the gradle.properties file.
        /// </summary>
        protected string GradlePropertiesLineStart => $"# Properties for \"{DependantName}\". This section is automatically generated.";

        /// <summary>
        /// The line that marks the end of the properties section in the gradle.properties file.
        /// </summary>
        protected string GradlePropertiesLineEnd => $"# End of properties for \"{DependantName}\".";

        protected bool IsEnabled { get; set; } = true;

        void IPostGenerateGradleAndroidProject.OnPostGenerateGradleAndroidProject(string path)
        {
            if (string.IsNullOrEmpty(DependantName))
            {
                return;
            }

            if (SessionState.GetBool(k_AndroidDependenciesDisableKey, false) ||
                SessionState.GetBool($"{k_AndroidDependenciesDisableKey}:{DependantName}", false))
            {
                IsEnabled = false;
            }

            var buildGradle = Path.Combine(path, "build.gradle");

            if (GetUnityVersionAsFloat(Application.unityVersion) >= 2022.2f)
            {
                var settingsGradle = Path.Combine(path, "..", "settings.gradle");
                InjectGradleRepositoriesInSettings(settingsGradle);
            }
            else
            {
                InjectGradleRepositoriesInUnityLib(buildGradle);
            }

            InjectGradleDependencies(buildGradle);

            var gradleProperties = Path.Combine(path, "..", "gradle.properties");
            InjectGradleProperties(gradleProperties);
        }

        /// <summary>
        /// Injects the dependencies into the build.gradle file.
        /// </summary>
        void InjectGradleDependencies(string buildGradle)
        {
            var dependencies = GenerateGradleDependencies();
            var startLine = DependenciesLineStart;
            var endLine = DependenciesLineEnd;

            ReplaceOrAddSectionFile(buildGradle, startLine, endLine, dependencies);
        }

        /// <summary>
        /// Injects the repositories into the settings.gradle file.
        /// </summary>
        /// <param name="settingsGradle">The path to the settings.gradle file.</param>
        void InjectGradleRepositoriesInSettings(string settingsGradle)
        {
            var repositories = GenerateGradleRepositoriesForSettings();
            var startLine = RepositoriesLineStart;
            var endLine = RepositoriesLineEnd;

            ReplaceOrAddSectionFile(settingsGradle, startLine, endLine, repositories);
        }

        /// <summary>
        /// Injects the repositories into the build.gradle file.
        /// </summary>
        /// <param name="buildGradle">The path to the build.gradle file.</param>
        void InjectGradleRepositoriesInUnityLib(string buildGradle)
        {
            var repositories = GenerateGradleRepositoriesForUnityLib();
            var startLine = RepositoriesLineStart;
            var endLine = RepositoriesLineEnd;

            ReplaceOrAddSectionFile(buildGradle, startLine, endLine, repositories);
        }

        /// <summary>
        /// Injects the properties into the gradle.properties file.
        /// </summary>
        /// <param name="gradleProperties">The path to the gradle.properties file.</param>
        void InjectGradleProperties(string gradleProperties)
        {
            var properties = GenerateGradleProperties();
            var startLine = GradlePropertiesLineStart;
            var endLine = GradlePropertiesLineEnd;

            ReplaceOrAddSectionFile(gradleProperties, startLine, endLine, properties);
        }

        void ReplaceOrAddSectionFile(string filename, string sectionStart, string sectionEnd, string sectionNewContent)
        {
            var content = File.ReadAllText(filename);
            content = ReplaceOrAddSectionString(content, sectionStart, sectionEnd, sectionNewContent);
            File.WriteAllText(filename, content);
        }

        string ReplaceOrAddSectionString(string content, string sectionStart, string sectionEnd, string sectionNewContent)
        {
            if (content.Contains(sectionStart))
            {
                var startIndex = content.IndexOf(sectionStart, StringComparison.Ordinal);
                var endIndex = content.IndexOf(sectionEnd, StringComparison.Ordinal) + sectionEnd.Length;
                var oldSection = content.Substring(startIndex, endIndex - startIndex);
                var newSection = $"{sectionStart}\n{sectionNewContent}\n{sectionEnd}";
                content = content.Replace(oldSection, newSection);
            }
            else
            {
                content += $"\n{sectionStart}\n{sectionNewContent}\n{sectionEnd}";
            }

            return content;
        }

        /// <summary>
        /// Generates the dependencies to be added to the build.gradle file.
        /// </summary>
        /// <returns>A string containing the dependencies to be added to the build.gradle file.</returns>
        protected string GenerateGradleDependencies()
        {
            var dependencies = Dependencies;
            if (dependencies == null || dependencies.Count == 0)
            {
                return "";
            }

            var dependencyLines = string.Join("\n", dependencies
                .Distinct()
                .Select(dependency => $"        implementation '{dependency}'"));

            var dependencyBlock = $@"afterEvaluate {{
    dependencies {{
{dependencyLines}
    }}
}}";

            if (!IsEnabled)
            {
                dependencyBlock = CommentBlock(dependencyBlock, "//");
            }

            return dependencyBlock;
        }

        /// <summary>
        /// Generates the repositories to be added to the settings.gradle file.
        /// </summary>
        /// <returns>A string containing the repositories to be added to the settings.gradle file.</returns>
        protected string GenerateGradleRepositoriesForSettings()
        {
            var repositories = Repositories;
            if (repositories == null || repositories.Count == 0)
            {
                return "";
            }

            var repositoriesBlock = string.Join("\n", repositories
                .Distinct()
                .Select(repository =>
                    $"settings.getDependencyResolutionManagement().getRepositories().maven(mavenRepository -> {{ mavenRepository.setUrl('{repository}'); }})"));

            if (!IsEnabled)
            {
                repositoriesBlock = CommentBlock(repositoriesBlock, "//");
            }

            return repositoriesBlock;
        }

        /// <summary>
        /// Generates the repositories to be added to the build.gradle file.
        /// </summary>
        /// <returns>A string containing the repositories to be added to the build.gradle file.</returns>
        protected string GenerateGradleRepositoriesForUnityLib()
        {
            var repositories = Repositories;
            if (repositories == null || repositories.Count == 0)
            {
                return "";
            }

            var repositoryLines = string.Join("\n", repositories
                .Distinct()
                .Select(repository => $"        maven {{ url \"{repository}\" }}"));

            var repositoryBlock = $@"([rootProject] + (rootProject.subprojects as List)).each {{ project ->
    project.repositories {{
{repositoryLines}
    }}
}}";

            if (!IsEnabled)
            {
                repositoryBlock = CommentBlock(repositoryBlock, "//");
            }

            return repositoryBlock;
        }

        /// <summary>
        /// Generates the properties to be added to the gradle.properties file.
        /// </summary>
        /// <returns>A string containing the properties to be added to the gradle.properties file.</returns>
        protected string GenerateGradleProperties()
        {
            var properties = GradleProperties;
            if (properties == null || properties.Count == 0)
            {
                return "";
            }

            var propertiesBlock = string.Join("\n", properties);
            if (!IsEnabled)
            {
                propertiesBlock = CommentBlock(propertiesBlock, "#");
            }

            return propertiesBlock;
        }

        public static string CommentBlock(string block, string commentPrefix)
        {
            return string.Join("\n", block.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(line => $"{commentPrefix} {line.TrimEnd()}"));
        }

        /// <summary>
        /// A helper method to extract dependencies from xml files.
        /// </summary>
        /// <param name="files">The list of xml files to extract dependencies from.</param>
        /// <returns>A list of dependencies and repositories extracted from the xml files.</returns>
        protected static AndroidXmlDependencies DependenciesFromXmlFiles(string[] files)
        {
            var dependencies = new AndroidXmlDependencies
            {
                Repositories = new List<string>(),
                Dependencies = new List<string>()
            };

            foreach (var file in files)
            {
                var fileDependencies = DependenciesFromXmlFile(file);
                dependencies.Dependencies.AddRange(fileDependencies.Dependencies);
                dependencies.Repositories.AddRange(fileDependencies.Repositories);
            }

            return dependencies;
        }

        /// <summary>
        /// A helper method to extract dependencies from an xml file.
        /// </summary>
        /// <param name="file">The xml file to extract dependencies from.</param>
        /// <returns>A list of dependencies and repositories extracted from the xml file.</returns>
        protected static AndroidXmlDependencies DependenciesFromXmlFile(string file)
        {
            if (!File.Exists(file))
            {
                return null;
            }
            var dependencies = new AndroidXmlDependencies
            {
                Repositories = new List<string>(),
                Dependencies = new List<string>()
            };

            XDocument doc = XDocument.Load(file);

            var androidPackages = doc.Descendants("androidPackage")
                .Select(package => new
                {
                    Spec = package.Attribute("spec")?.Value,
                    Repositories = package.Descendants("repository").Select(repo => repo.Value).ToList()
                }).ToList();

            foreach (var androidPackage in androidPackages)
            {
                if (androidPackage.Spec != null)
                {
                    dependencies.Dependencies.Add(androidPackage.Spec);
                }

                dependencies.Repositories.AddRange(androidPackage.Repositories);
            }

            return dependencies;
        }

        public virtual float GetUnityVersionAsFloat(string unityVersion)
        {
            if (string.IsNullOrEmpty(unityVersion))
            {
                return 0f;
            }
            var versionParts = unityVersion.Split('.');
            if (versionParts.Length < 2)
            {
                return 0f;
            }

            if (!float.TryParse($"{versionParts[0]}.{versionParts[1]}", out var version))
            {
                return 0f;
            }

            return version;
        }

        public class AndroidXmlDependencies
        {
            public List<string> Dependencies { get; set; }
            public List<string> Repositories { get; set; }
        }
    }
}
