﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using Octokit;
using PoeHUD.Framework;
using PoeHUD.Hud;
using PoeHUD.Hud.Menu;
using PoeHUD.Hud.PluginExtension;
using PoeHUD.Plugins;
using SharpDX;
using SharpDX.Direct3D9;


namespace PoeHUD_PluginsUpdater
{
    public sealed class GitConfigParser
    {
        public const string ConfigFileName = "GitUpdateConfig.txt";

        private const string OPTION_OWNER = "Owner:";
        private const string OPTION_REPONAME = "Name:";
        private const string OPTION_RELEASE = "Release";
        private const string OPTION_RELEASETAGREGEXFILTER = "Tag:";
        private const string OPTION_REPOONLY = "Repository";
        private const string OPTION_REPOBRANCH = "Branch:";
        private const string OPTION_FILESIGNORE = "Ignore:";

        public static void Parse(PluginToUpdate plugVariant)
        {
            try
            {
                var gitConfigFilePath = Path.Combine(plugVariant.PluginDirectory, ConfigFileName);

                if (File.Exists(gitConfigFilePath))
                {
                    plugVariant.bHasGitConfig = true;
                    var configLines = File.ReadAllLines(gitConfigFilePath);



                    var handleIgnore = false;
                    for (var i = 0; i < configLines.Length; i++)
                    {
                        var line = configLines[i];
                        if (line.StartsWith("#")) continue;

                        var spacelessLine = line.Replace(" ", "");
                        if (spacelessLine.Replace("\r", "").Replace("\n", "").Length == 0) continue;

                        if (handleIgnore)
                        {
                            plugVariant.IgnoredEntities.Add(line);
                            continue;
                        }
                        if (spacelessLine == OPTION_FILESIGNORE)
                        {
                            handleIgnore = true;
                            continue;
                        }

                        //Repository owner
                        var ownerIndex = line.IndexOf(OPTION_OWNER);
                        if (ownerIndex != -1)
                        {
                            plugVariant.RepoOwner = line.Substring(ownerIndex + OPTION_OWNER.Length);
                            TrimName(ref plugVariant.RepoOwner);
                            continue;
                        }

                        //Repository name
                        var reposNameIndex = line.IndexOf(OPTION_REPONAME);
                        if (reposNameIndex != -1)
                        {
                            plugVariant.RepoName = line.Substring(reposNameIndex + OPTION_REPONAME.Length);
                            TrimName(ref plugVariant.RepoName);
                            continue;
                        }

                        //Only from release
                        if (spacelessLine == OPTION_RELEASE)
                        {
                            if (plugVariant.UpdateVariant != ePluginSourceOfUpdate.Undefined)
                                BasePlugin.LogMessage(
                                    "PluginUpdater: " + plugVariant.PluginName +
                                    ",  both update variants (Release and Commit) is not allowed. Check GitUpdateConfig. Current update variant is: " +
                                    plugVariant.UpdateVariant, 10);
                            else
                                plugVariant.UpdateVariant = ePluginSourceOfUpdate.Release;
                            continue;
                        }

                        //Only from repository
                        if (spacelessLine == OPTION_REPOONLY)
                        {
                            if (plugVariant.UpdateVariant != ePluginSourceOfUpdate.Undefined)
                                BasePlugin.LogMessage(
                                    "PluginUpdater: " + plugVariant.PluginName +
                                    ",  both update variants (Release and Commit) is not allowed. Check GitUpdateConfig. Current update variant is: " +
                                    plugVariant.UpdateVariant, 10);
                            else
                                plugVariant.UpdateVariant = ePluginSourceOfUpdate.RepoBranch;
                            continue;
                        }

                        //Release tag regex filter
                        var tagIndex = line.IndexOf(OPTION_RELEASETAGREGEXFILTER);
                        if (tagIndex != -1)
                        {
                            plugVariant.ReleaseRegexTag = line.Substring(tagIndex + OPTION_RELEASETAGREGEXFILTER.Length);
                            TrimName(ref plugVariant.ReleaseRegexTag);
                            plugVariant.bCustomTag = true;
                        }

                        var branchNameIndex = line.IndexOf(OPTION_REPOBRANCH);
                        if (branchNameIndex != -1)
                        {
                            plugVariant.BranchName = line.Substring(branchNameIndex + OPTION_REPOBRANCH.Length);
                            TrimName(ref plugVariant.BranchName);
                        }
                    }

                    plugVariant.bAllowCheckUpdate = true;

                    if (string.IsNullOrEmpty(plugVariant.RepoOwner))
                    {
                        BasePlugin.LogError("PluginUpdater: Repository Owner is not defined in plugin: " + plugVariant.PluginName, 10);
                        plugVariant.UpdateState = ePluginUpdateState.WrongConfig;
                        plugVariant.bAllowCheckUpdate = false;
                    }
                    if (string.IsNullOrEmpty(plugVariant.RepoName))
                    {
                        BasePlugin.LogError("PluginUpdater: Repository Name is not defined in plugin: " + plugVariant.PluginName, 10);
                        plugVariant.UpdateState = ePluginUpdateState.WrongConfig;
                        plugVariant.bAllowCheckUpdate = false;
                    }
                    if (plugVariant.UpdateVariant == ePluginSourceOfUpdate.Undefined)
                    {
                        BasePlugin.LogError("PluginUpdater: Update type (Release or Repository) is not defined in plugin: " + plugVariant.PluginName, 10);
                        plugVariant.UpdateState = ePluginUpdateState.WrongConfig;
                        plugVariant.bAllowCheckUpdate = false;
                    }
                     
                }
            }
            catch
            {
                BasePlugin.LogError("PluginUpdater: Error while parsing git update config for plugin: " + plugVariant.PluginName, 10);
            }
        }

        private static void TrimName(ref string name)
        {
            name = name.TrimEnd(' ');
            name = name.TrimStart(' ');
        }

    }
}
