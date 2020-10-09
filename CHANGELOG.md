# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [3.0.0-pre.3] - 2020-10-09
First integration into Unity 2021

## [3.0.0-preview.9] - 2020-10-09

Google Billing v3
For internal test.

## [3.0.0-preview.8] - 2020-04-28

For internal test. Final set of functional changes for initial public release. Doc updates still inbound.

## [3.0.0-preview.7] - 2020-04-20

Updated npm URLs to the new host on Artifactory
Added hard dependency on the ugui package for newer Editors

## [3.0.0-preview.6] - 2020-04-09

Cleans up some missed .meta files

## [3.0.0-preview.5] - 2020-04-09

Weekly internal preview. Includes store deprecation and cleanup of conflicting core code.

## [3.0.0-preview.4] - 2020-04-02
## [3.0.0-preview.3] - 2020-04-02

Testing from develop post-merge. This is basically 1.24 plus a change to facilitate UDP decoupling.

## [3.0.0-preview.2] - 2020-03-19

## [3.0.0-preview.1] - 2020-03-02

## [3.0.0-preview] - 2020-02-07

### This is the first release of the Unified *Unity In App Purchasing*, combining the old package and its Asset Store Components.

## [2.2.0-preview.1] - 2020-10-22
Google Billing v3

## [2.1.2] - 2020-09-20
Fix migration tooling's obfuscator file destination path to target Scripts instead of Resources

## [2.1.1] - 2020-08-25
Fix compilation compatibility with platforms that don't use Unity Analytics (ex: PS4)
Fix compilation compatibility with "Scripting Runtime Version" option set to ".Net 3.5 Equivalent (Deprecated)" in Unity 2018.4

## [2.1.0] - 2020-06-29
Source Code provided instead of precompiled dlls.
Live vs Stub DLLs are now using asmdef files to differentiate their targeting via the Editor
Fixed errors regarding failing to find assemblies when toggling In-App Purchasing in the Service Window or Purchasing Service Settings
Fixed failure to find UI assemblies when updating the Editor version.
Added menu to support eventual migration to In-App Purchasing version 3.

## [2.0.6] - 2019-02-18
Remove embedded prebuilt assemblies.

## [2.0.5] - 2019-02-08
Fixed Unsupported platform error

## [2.0.4] - 2019-01-20
Added editor and playmode testing.

## [2.0.3] - 2018-06-14
Fixed issue related to 2.0.2 that caused new projects to not compile in the editor. 
Engine dll is enabled for editor by default.
Removed meta data that disabled engine dll for windows store.

## [2.0.2] - 2018-06-12
Fixed issue where TypeLoadException occured while using "UnityEngine.Purchasing" because SimpleJson was not found. fogbugzId: 1035663.

## [2.0.1] - 2018-02-14
Fixed issue where importing the asset store package would fail due to importer settings.

## [2.0.0] - 2018-02-07
Fixed issue with IAP_PURCHASING flag not set on project load.
