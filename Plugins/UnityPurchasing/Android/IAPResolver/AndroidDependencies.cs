using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using UnityEditor;
using UnityEditor.Android;

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
            InjectGradleDependencies(buildGradle);

            var settingsGradle = Path.Combine(path, "..", "settings.gradle");
            InjectGradleRepositories(settingsGradle);

            var gradleProperties = Path.Combine(path, "..", "gradle.properties");
            InjectGradleProperties(gradleProperties);
        }

        /// <summary>
        /// Injects the dependencies into the build.gradle file.
        /// </summary>
        void InjectGradleDependencies(string buildGradle)
        {
            var dependencies = GenerateGradleDependencies();
            if (dependencies == null)
            {
                return;
            }

            var startLine = DependenciesLineStart;
            var endLine = DependenciesLineEnd;

            ReplaceOrAddSectionFile(buildGradle, startLine, endLine, dependencies);
        }

        /// <summary>
        /// Injects the repositories into the settings.gradle file.
        /// </summary>
        /// <param name="settingsGradle">The path to the settings.gradle file.</param>
        void InjectGradleRepositories(string settingsGradle)
        {
            var repositories = GenerateGradleRepositories();
            if (repositories == null)
            {
                return;
            }

            var startLine = RepositoriesLineStart;
            var endLine = RepositoriesLineEnd;

            ReplaceOrAddSectionFile(settingsGradle, startLine, endLine, repositories);
        }

        /// <summary>
        /// Injects the properties into the gradle.properties file.
        /// </summary>
        /// <param name="gradleProperties">The path to the gradle.properties file.</param>
        void InjectGradleProperties(string gradleProperties)
        {
            var properties = GenerateGradleProperties();
            if (properties == null)
            {
                return;
            }

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
                return null;
            }

            return string.Join("\n", Dependencies
                .Distinct()
                .Select(dependency => $"project.getDependencies().add('implementation', '{dependency}')")
                .Select(dependency => IsEnabled ? dependency : $"// {dependency}"));
        }

        /// <summary>
        /// Generates the repositories to be added to the build.gradle file.
        /// </summary>
        /// <returns>A string containing the repositories to be added to the build.gradle file.</returns>
        protected string GenerateGradleRepositories()
        {
            var repositories = Repositories;
            if (repositories == null || repositories.Count == 0)
            {
                return null;
            }

            return string.Join("\n", repositories
                .Distinct()
                .Select(repository =>
                    $"settings.getDependencyResolutionManagement().getRepositories().maven(mavenRepository -> {{ mavenRepository.setUrl('{repository}'); }})")
                .Select(repository => IsEnabled ? repository : $"// {repository}"));
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
                return null;
            }

            return string.Join("\n", properties.Select(property => IsEnabled ? property : $"# {property}"));
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

        public class AndroidXmlDependencies
        {
            public List<string> Dependencies { get; set; }
            public List<string> Repositories { get; set; }
        }
    }
}
